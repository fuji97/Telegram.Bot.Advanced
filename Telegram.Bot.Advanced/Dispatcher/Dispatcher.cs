using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Dispatcher.Filters;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Dispatcher
{
    /// <summary>
    /// The dispatcher class that receive the Update, create and set the controller and call the correct method
    /// </summary>
    /// <typeparam name="TContext">Context used by the application</typeparam>
    /// <typeparam name="TController">Controller to use as source of methods</typeparam>
    public class Dispatcher<TContext, TController> : IDisposable, IDispatcher
        where TContext : TelegramContext 
        where TController : class, ITelegramController<TContext> {
        
        private readonly Dictionary<MethodInfo, Type> _methods;
        private readonly KeyValuePair<MethodInfo, Type> _noMethodsMethod;
        private ILogger _logger;
        private readonly ITelegramBotData _botData;
        private int _lastUpdateId = -1;

        public Dispatcher([NotNull] ITelegramBotData botData, ILogger<Dispatcher<TContext,TController>> logger = null, List<Type> controllers = null) {
            _botData = botData ?? throw new ArgumentNullException(nameof(botData), "botData cannot be null");
            _methods = new Dictionary<MethodInfo, Type>();
            var methodInfos = typeof(TController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName);
            
            foreach (var methodInfo in methodInfos) {
                _methods.Add(methodInfo, typeof(TController));
            }
            
            if (controllers != null) {
                foreach (var controller in controllers) {
                    if (controller.GetInterfaces().Contains(typeof(ITelegramController<TContext>))) {
                        methodInfos = controller
                            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            .Where(m => !m.IsSpecialName);
                        foreach (var methodInfo in methodInfos) {
                            _methods.Add(methodInfo, controller);
                        }
                    }
                }
            }
            _noMethodsMethod = _methods.FirstOrDefault(m => m.Key.GetCustomAttribute<NoMethodFilter>() != null);
            _logger = logger;
        }

        private static void SetControllerData(TelegramController<TContext> controller, Update update, MessageCommand command,
            TContext context,
            TelegramChat chat, ITelegramBotData botData) {
            controller.Update = update;
            controller.MessageCommand = command;
            controller.TelegramContext = context;
            controller.TelegramChat = chat;
            controller.BotData = botData;
        }

        public Type GetControllerType() {
            return typeof(TController);
        }

        public Type GetContextType() {
            return typeof(TContext);
        }
        
        public void SetServices(IServiceProvider provider) {
            _logger = provider.GetService<ILogger<Dispatcher<TContext,TController>>>();
        }

        public async Task DispatchUpdateAsync(Update update, IServiceProvider provider) {
            if (_lastUpdateId != update.Id) {
                _lastUpdateId = update.Id;
                using (var scope = provider.CreateScope()) {
                    await DispatchAsync(scope, update);
                }
            }
            else {
                _logger.LogWarning("Duplicate update received - skipping");
            }
        }

        public void RegisterController(IServiceCollection services) {
            services.TryAddScoped<TController>();
        }

        private async Task DispatchAsync(IServiceScope scope, Update update) {
            
            TContext context = scope.ServiceProvider.GetRequiredService<TContext>();
            _logger.LogInformation($"Received update - ID: {update.Id}");
            TelegramChat chat = await UpdateChat(update, context);

            MessageCommand command;
            if (update.Type == UpdateType.Message)
            {
                command = new MessageCommand(update.Message);
                if (command.Target != null && _botData?.Username != null && command.Target != _botData.Username)
                {
                    _logger.LogDebug($"Command's target is @{command.Target} - Ignoring command");
                    return;
                }
            }
            else {
                command = new MessageCommand();
            }
            
            // Update data
            switch (_botData!.UserUpdate) {
                case UserUpdate.BotCommand:
                    if (command.IsCommand())
                        await UpdateUser(update, context);
                    break;
                case UserUpdate.PrivateMessage:
                    if (update.Message.Chat.Type == ChatType.Private) {
                        //await UpdateUser(update, context);
                    }
                    //await UpdateUser(update, context);
                    
                    break;
            }
            //await UpdateChat(update, context);
            
            await context.SaveChangesAsync();

            var firstMethod = FindFirstMethod(update, chat, command);

            // TODO Too much spaghetti code, need to fix this, the null check should be done inside of ExecuteMethod
            if (firstMethod.Key != null) {
                TelegramController<TContext> controller = (TelegramController<TContext>) scope.ServiceProvider.GetRequiredService(firstMethod.Value);

                SetControllerData(controller, update, command, context, chat, _botData);

                _logger.LogTrace($"Command: {JsonConvert.SerializeObject(command, Formatting.Indented)}");
                _logger.LogTrace($"Chat: {JsonConvert.SerializeObject(chat, Formatting.Indented)}");
            
                await ExecuteMethod(firstMethod.Key, controller, firstMethod.Value);
            }
            
            _logger.LogTrace("End of dispatching");
        }

        // TODO Change all TelegramContext to ITelegramContext
        private async Task ExecuteMethod(MethodInfo firstMethod, TelegramController<TContext> controller, Type controllerType) {
            if (firstMethod != null) {
                if (firstMethod.GetCustomAttribute<AsyncStateMachineAttribute>() != null) {
                    _logger.LogInformation($"Calling async method: {firstMethod.Name}");
                    await (Task) firstMethod.Invoke(Convert.ChangeType(controller, controllerType), null);
                }
                else {
                    _logger.LogInformation($"Calling sync method: {firstMethod.Name}");
                    firstMethod.Invoke(controller, null);
                }
            }
            // TODO Re-enable _noMethodsMethod
            /*else if (_noMethodsMethod != null) {
                if (_noMethodsMethod.Key.GetCustomAttribute<AsyncStateMachineAttribute>() != null) {
                    _logger.LogInformation("No valid method found to manage current request. Calling the async NoMethod: " +
                                           _noMethodsMethod.Key.Name);
                    await (Task) _noMethodsMethod.Invoke(controller, null);
                }
                else {
                    _logger.LogInformation("No valid method found to manage current request. Calling the sync NoMethod: " +
                                           _noMethodsMethod.Name);
                    try {
                        _noMethodsMethod.Invoke(controller, null);
                    }
                    catch (ApiRequestException e) {
                        Console.WriteLine(e);
                    }
                }
            }*/
            else {
                _logger.LogInformation("No valid method found to manage current request.");
            }
        }

        private KeyValuePair<MethodInfo, Type> FindFirstMethod(Update update, TelegramChat chat, MessageCommand command) {
            var firstMethod = _methods.FirstOrDefault(m => ((MemberInfo) m.Key).GetCustomAttributes()
                .Where(att => att is DispatcherFilterAttribute)
                .All(attr =>
                    ((DispatcherFilterAttribute) attr).IsValid(update,
                        chat, command, _botData)));
            return firstMethod;
        }

        private async Task<TelegramChat> UpdateChat(Update update, TContext context) {
            TelegramChat chat = null;
            if (update.Type == UpdateType.Message) {
                var newChat = update.Message.Chat;
                chat = await TelegramChat.GetAsync(context, newChat.Id);

                if (chat != null) {
                    if (newChat.Username != null && chat.Username == newChat.Username) {
                        chat.Username = newChat.Username;
                    }

                    if (newChat.Title != null && chat.Title == newChat.Title) {
                        chat.Title = newChat.Title;
                    }

                    if (newChat.Description != null && chat.Description == newChat.Description) {
                        chat.Description = newChat.Description;
                    }

                    if (newChat.InviteLink != null && chat.InviteLink == newChat.InviteLink) {
                        chat.InviteLink = newChat.InviteLink;
                    }

                    if (newChat.LastName != null && chat.LastName == newChat.LastName) {
                        chat.LastName = newChat.LastName;
                    }

                    if (newChat.StickerSetName != null && chat.StickerSetName == newChat.StickerSetName) {
                        chat.StickerSetName = newChat.StickerSetName;
                    }

                    if (newChat.FirstName != null && chat.FirstName == newChat.FirstName) {
                        chat.FirstName = newChat.FirstName;
                    }

                    if (newChat.CanSetStickerSet != null && chat.CanSetStickerSet == newChat.CanSetStickerSet) {
                        chat.CanSetStickerSet = newChat.CanSetStickerSet;
                    }
                }
                else {
                    chat = new TelegramChat(update.Message.Chat);
                    await context.AddAsync(chat);
                }
            }

            return chat;
        }

        private async Task UpdateUser(Update update, TContext context) {
            var updateUser = update.Message?.From;
            if (updateUser != null) {
                var storedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == updateUser.Id);
                if (storedUser != null) {
                    if (updateUser.Username != null) storedUser.Username = updateUser.Username;
                    if (updateUser.FirstName != null) storedUser.FirstName = updateUser.FirstName;
                    if (updateUser.LastName != null) storedUser.LastName = updateUser.LastName;
                    
                    // TODO Check when the user is tracked as modified
                }
            }
        }

        public void Dispose() {
            
        }
    }
}
