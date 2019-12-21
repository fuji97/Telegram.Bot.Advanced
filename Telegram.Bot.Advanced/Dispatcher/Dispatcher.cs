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
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Dispatcher.Filters;
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
        private readonly IEnumerable<MethodInfo> _methods;
        private readonly MethodInfo _noMethodsMethod;
        private ILogger _logger;
        private readonly ITelegramBotData _botData;
        private int _lastUpdateId = -1;

        public Dispatcher(ITelegramBotData botData, ILogger<Dispatcher<TContext,TController>> logger = null) {
            _botData = botData;
            _methods = typeof(TController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName);
            _noMethodsMethod = _methods.FirstOrDefault(m => m.GetCustomAttribute<NoMethodFilter>() != null);
            _logger = logger;
        }

        private static void SetControllerData(TController controller, Update update, MessageCommand command,
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
            TController controller = scope.ServiceProvider.GetRequiredService<TController>();
            TContext context = scope.ServiceProvider.GetRequiredService<TContext>();
            _logger.LogInformation($"Received update - ID: {update.Id}");
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

            MessageCommand command;
            if (update.Type == UpdateType.Message)
            {
                command = new MessageCommand(update.Message);
                if (command.Target != null && _botData.Username != null && command.Target != _botData.Username)
                {
                    _logger.LogDebug($"Command's target is @{command.Target} - Ignoring command");
                    return;
                }
            }
            else {
                command = new MessageCommand();
            }

            SetControllerData(controller, update, command, context, chat, _botData);
            
            context.SaveChanges();
            
            _logger.LogTrace($"Command: {JsonConvert.SerializeObject(command, Formatting.Indented)}");
            _logger.LogTrace($"Chat: {JsonConvert.SerializeObject(chat, Formatting.Indented)}");
            
            var firstMethod = _methods.FirstOrDefault(m => ((MemberInfo) m).GetCustomAttributes()
                .Where(att => att is DispatcherFilterAttribute)
                .All(attr =>
                    ((DispatcherFilterAttribute) attr).IsValid(update,
                        chat, command, _botData)));
            
            if (firstMethod != null) {
                
                if (firstMethod.GetCustomAttribute<AsyncStateMachineAttribute>() != null) {
                    _logger.LogInformation($"Calling async method: {firstMethod.Name}");
                    await (Task) firstMethod.Invoke(controller, null);
                }
                else {
                    _logger.LogInformation($"Calling sync method: {firstMethod.Name}");
                    firstMethod.Invoke(controller, null);
                }
            } else if (_noMethodsMethod != null) {
                if (_noMethodsMethod.GetCustomAttribute<AsyncStateMachineAttribute>() != null) {
                    _logger.LogInformation("No valid method found to manage current request. Calling the async NoMethod: " + _noMethodsMethod.Name);
                    await (Task) _noMethodsMethod.Invoke(controller, null);
                }
                else {
                    _logger.LogInformation("No valid method found to manage current request. Calling the sync NoMethod: " + _noMethodsMethod.Name);
                    try {
                        _noMethodsMethod.Invoke(controller, null);
                    }
                    catch (ApiRequestException e) {
                        Console.WriteLine(e);
                    }
                }
            } else {
                _logger.LogInformation("No valid method found to manage current request.");
            }
            _logger.LogTrace("End of dispatching");
        }

        public void Dispose() {
            
        }
    }
}
