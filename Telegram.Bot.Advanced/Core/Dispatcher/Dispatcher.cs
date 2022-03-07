using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Core.Dispatcher
{
    /// <summary>
    /// The dispatcher class that receive the Update, create and set the controller and call the correct method
    /// </summary>
    /// <typeparam name="TContext">Context used by the application</typeparam>
    /// <typeparam name="TController">Controller to use as source of methods</typeparam>
    public class Dispatcher<TContext, TController> : Dispatcher<TContext>
        where TContext : TelegramContext 
        where TController : class, ITelegramController<TContext> {
        
        public Dispatcher(ITelegramBotData botData, IList<Type> controllers, ILogger<Dispatcher<TContext, TController>>? logger = null) : base(botData, controllers, logger) {
            Controllers.Add(typeof(TController));
            
            var methodInfos = typeof(TController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName);
            foreach (var methodInfo in methodInfos) {
                Methods.Add(methodInfo, typeof(TController));
            }

            NoMethodsMethod = Methods.FirstOrDefault(m => m.Key.GetCustomAttribute<NoMethodFilter>() != null);
        }

        // TODO Is this really necessary? Maybe get the scoped logger directly during dispatching, is logger scoped?
        public override void SetServices(IServiceProvider provider) {
            Logger = provider.GetService<ILogger<Dispatcher<TContext, TController>>>();
        }
    }

    /// <summary>
    /// The dispatcher class that receive the Update, create and set the controller and call the correct method
    /// </summary>
    /// <typeparam name="TContext">Context used by the application</typeparam>
    public class Dispatcher<TContext> : IDisposable, IDispatcher
        where TContext : TelegramContext {
        
        protected Dictionary<MethodInfo, Type> Methods;
        protected KeyValuePair<MethodInfo, Type>? NoMethodsMethod;
        protected ILogger? Logger;
        protected readonly ITelegramBotData BotData;
        protected int LastUpdateId = -1;
        protected IList<Type> Controllers;

        public Dispatcher(ITelegramBotData botData, IList<Type> controllers, ILogger<Dispatcher<TContext>>? logger = null) {
            BotData = botData ?? throw new ArgumentNullException(nameof(botData), "botData cannot be null");
            Methods = new Dictionary<MethodInfo, Type>();
            Controllers = controllers ?? throw new ArgumentNullException(nameof(controllers), "controllers cannot be null");
            
            foreach (var controller in controllers) {
                if (IsValidController(controller)) {
                    var methodInfos = controller
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Where(m => !m.IsSpecialName);
                    foreach (var methodInfo in methodInfos) {
                        Methods.Add(methodInfo, controller);
                    }
                }
                else {
                    throw new InvalidControllerException(
                        $"{controller.FullName} is not a valid controller. Make sure that the controller is " +
                        $"an instantiable class that implements ITelegramController<{typeof(TContext).Name}>");
                }
            }
            
            NoMethodsMethod = Methods.FirstOrDefault(m => m.Key.GetCustomAttribute<NoMethodFilter>() != null);
            Logger = logger;
        }

        private static bool IsValidController(Type controller) {
            // TODO Maybe add others controls like IsGenericParameter or IsEnum
            return controller.GetInterfaces().Contains(typeof(ITelegramController<TContext>)) && 
                   controller.IsClass &&
                   !controller.IsAbstract;
        }

        private static void SetControllerData(ITelegramController<TContext> controller, Update update, MessageCommand command,
            TContext context,
            TelegramChat? chat, ITelegramBotData botData) {
            controller.Update = update;
            controller.MessageCommand = command;
            controller.TelegramContext = context;
            controller.TelegramChat = chat;
            controller.BotData = botData;
        }

        public virtual IList<Type> GetControllersType() {
            return Controllers;
        }

        public virtual Type GetContextType() {
            return typeof(TContext);
        }
        
        public virtual void SetServices(IServiceProvider provider) {
            Logger = provider.GetService<ILogger<Dispatcher<TContext>>>();
        }

        public Task HandleErrorAsync(Exception e, IServiceProvider provider) {
            Logger?.LogError(e, "Telegram APIs raised an error");
            return Task.CompletedTask;
        }

        public virtual async Task DispatchUpdateAsync(Update update, IServiceProvider provider) {
            if (LastUpdateId != update.Id) {
                LastUpdateId = update.Id;
                using (var scope = provider.CreateScope()) {
                    await DispatchAsync(scope, update);
                }
            }
            else {
                Logger?.LogWarning("Duplicate update received - skipping");
            }
        }

        public virtual void RegisterController(IServiceCollection services) {
            foreach (var controller in Controllers) {
                services.TryAddScoped(controller);
            }
        }

        private async Task DispatchAsync(IServiceScope scope, Update update) {
            TContext context = scope.ServiceProvider.GetRequiredService<TContext>();
            Logger?.LogInformation("Received update - ID: {Id}", update.Id);
            TelegramChat? chat = await UpdateChat(update, context);

            // Ignore the update based on initial options
            MessageCommand command;
            if (update.Type == UpdateType.Message) {
                var ignoreBehaviour = update.Message!.Chat.IsGroup()
                    ? BotData.GroupChatBehaviour
                    : BotData.PrivateChatBehaviour;
                command = new MessageCommand(update.Message);

                switch (ignoreBehaviour) {
                    case IgnoreBehaviour.IgnoreAllMessages:
                        return;
                    case IgnoreBehaviour.IgnoreNonCommandMessages:
                        if (!command.IsCommand())
                            return;
                        break;
                    case IgnoreBehaviour.IgnoreAllMessagesAndCommandsWithoutTarget:
                        if (command.Target == null)
                            return;
                        break;
                    case IgnoreBehaviour.IgnoreNothing:
                        break;
                }
            }
            else {
                command = new MessageCommand();
            }
            
            // Update data
            switch (BotData!.UserUpdate) {
                case UserUpdate.BotCommand:
                    if (command.IsCommand())
                        await UpdateUser(update, context, chat);
                    break;
                case UserUpdate.EveryMessage:
                    await UpdateUser(update, context, chat);
                    break;
            }

            await context.SaveChangesAsync();

            Dictionary<MethodInfo, Type> eligibleControllers;
            KeyValuePair<MethodInfo, Type>? firstMethod;

            try {
                eligibleControllers = FindEligibleControllers(update, chat, command);
            }
            catch (Exception e) {
                Logger?.LogError(e, "An exception was thrown while filtering the eligible controllers");
                throw;
            }

            try {
                firstMethod = FindFirstMethod(eligibleControllers, update, chat, command);
            }
            catch (Exception e) {
                Logger?.LogError(e, "An exception was thrown while filtering the eligible methods");
                throw;
            }
            
            Logger?.LogTrace("Command: {Command}", JsonConvert.SerializeObject(command, Formatting.Indented));
            Logger?.LogTrace("Chat: {Chat}", JsonConvert.SerializeObject(chat, Formatting.Indented));

            try {
                await SetupAndExecute(scope, update, firstMethod, command, context, chat);
            }
            catch (Exception e) {
                Logger?.LogError(e, "An exception was thrown while dispatching the request");
                throw;
            }

            Logger?.LogTrace("End of dispatching");
        }

        private async Task SetupAndExecute(IServiceScope scope, Update update, KeyValuePair<MethodInfo, Type>? firstMethod, MessageCommand command,
            TContext context, TelegramChat? chat) {
            if (firstMethod.HasValue) {
                var method = firstMethod.Value; 
                var controller = (ITelegramController<TContext>) scope.ServiceProvider.GetRequiredService(method.Value);

                SetControllerData(controller, update, command, context, chat, BotData);
                await ExecuteMethod(method.Value, method.Key, controller);
            }
            else if (NoMethodsMethod.HasValue) {
                var method = NoMethodsMethod.Value;
                var controller = (TelegramController<TContext>) scope.ServiceProvider.GetRequiredService(method.Value);

                SetControllerData(controller, update, command, context, chat, BotData);
                await ExecuteMethod(method.Value, method.Key, controller);
            }
            else {
                Logger?.LogInformation("No valid method found to handle the current request");
            }
        }
        
        private async Task ExecuteMethod(Type controllerType, MethodInfo handler, ITelegramController<TContext> controller) {
            if (handler.GetCustomAttribute<AsyncStateMachineAttribute>() != null) {
                Logger?.LogInformation("Calling async method: {Name}", handler.Name);
                await (Task) handler.Invoke(Convert.ChangeType(controller, controllerType), null)!;
            }
            else {
                Logger?.LogInformation("Calling sync method: {Name}", handler.Name);
                handler.Invoke(controller, null);
            }
        }

        private Dictionary<MethodInfo, Type> FindEligibleControllers(Update update, TelegramChat? chat,
            MessageCommand command) {
            var eligibleControllers = Methods.Where(m => 
                    m.Value.GetCustomAttributes<DispatcherFilterAttribute>()
                        .All(attr => attr.IsValid(update, chat, command, BotData)
                    ))
                .ToDictionary(k => k.Key, v => v.Value);
            return eligibleControllers;
        }

        private KeyValuePair<MethodInfo, Type>? FindFirstMethod(Dictionary<MethodInfo, Type> methods, Update update, TelegramChat? chat, MessageCommand command) {
            KeyValuePair<MethodInfo, Type>? firstMethod = methods.FirstOrDefault(m => 
                m.Key.GetCustomAttributes<DispatcherFilterAttribute>()
                .All(attr => attr.IsValid(update, chat, command, BotData)));
            return firstMethod;
        }

        private async Task<TelegramChat?> UpdateChat(Update update, TContext context, TelegramChat? chat = null) {
            if (update.GetMessage()?.Chat == null) 
                return chat;
            
            var newChat = update.GetMessage()!.Chat;
            chat ??= await TelegramChat.GetAsync(context, newChat.Id);

            if (chat != null) {
                if (newChat.Username != null) chat.Username = newChat.Username;
                if (newChat.Title != null) chat.Title = newChat.Title;
                if (newChat.Description != null) chat.Description = newChat.Description;
                if (newChat.InviteLink != null) chat.InviteLink = newChat.InviteLink;
                if (newChat.LastName != null) chat.LastName = newChat.LastName;
                if (newChat.StickerSetName != null) chat.StickerSetName = newChat.StickerSetName;
                if (newChat.FirstName != null) chat.FirstName = newChat.FirstName;
                if (newChat.CanSetStickerSet != null) chat.CanSetStickerSet = newChat.CanSetStickerSet;
            }
            else {
                chat = new TelegramChat(newChat);
                    
                // Check if the chat has a default role and set it, if any
                var defaultRole = BotData.DefaultUserRole.FirstOrDefault(d => d.Equals(chat));
                if (defaultRole != null) {
                    chat.Role = defaultRole.Role;
                }
                    
                await context.AddAsync(chat);
            }

            return chat;
        }

        private async Task UpdateUser(Update update, TContext context, TelegramChat? currentChat) {
            var updateUser = update.GetMessage()?.From;

            if (updateUser?.Id == currentChat?.Id) {
                return;
            }
            
            if (updateUser != null) {
                var storedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == updateUser.Id);
                if (storedUser != null) {
                    storedUser.FirstName = updateUser.FirstName;
                    if (updateUser.Username != null) storedUser.Username = updateUser.Username;
                    if (updateUser.LastName != null) storedUser.LastName = updateUser.LastName;
                }
                else {
                    TelegramChat chat = updateUser.ToModel();
                    await context.AddAsync(chat);
                }
            }
        }

        public void Dispose() {
            
        }
    }
}
