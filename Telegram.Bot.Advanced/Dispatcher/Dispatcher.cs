using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.DispatcherFilters;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Dispatcher
{
    public class Dispatcher<TContext, TController> : IDisposable, IDispatcher
        where TContext : TelegramContext, new() 
        where TController : class, ITelegramController<TContext>, new() {
        private readonly IEnumerable<MethodInfo> _methods;
        private ILogger _logger;
        private readonly ITelegramBotData _botData;
        //private IServiceProvider provider;
        //private IServiceCollection services;
        //private IServiceScope scope;

        public Dispatcher(ITelegramBotData botData) {
            _botData = botData;
            _methods = typeof(TController).GetMethods(BindingFlags.Public | BindingFlags.Instance);
                                 //.Where(m => m.GetCustomAttributes(typeof(DispatcherFilterAttribute), false).Length > 0);

            //ILoggerFactory loggerFactory = new LoggerFactory();
            //_logger = loggerFactory.CreateLogger<Dispatcher<TContext, TController>>();
        }

        public void DispatchUpdate(Update update, IServiceProvider provider) {
            if (provider != null)
            {
                using (var scope = provider.CreateScope()) {
                    Dispatch(scope.ServiceProvider.GetRequiredService<TController>(), update);
                }
            }
            else {
                Dispatch(new TController(), update);
            }
            
        }

        public void SetServices(IServiceProvider provider) {
            _logger = provider.GetService<ILogger<Dispatcher<TContext,TController>>>();
        }

        public async Task DispatchUpdateAsync(Update update, IServiceProvider provider)
        {
            if (provider != null)
            {
                using (var scope = provider.CreateScope())
                {
                    await DispatchAsync(scope.ServiceProvider.GetRequiredService<TController>(), update);
                }
            }
            else
            {
                await DispatchAsync(new TController(), update);
            }
        }

        public void RegisterController(IServiceCollection services) {
            services.TryAddScoped<TController>();
        }

        private static void SetControllerData(TController controller, MessageCommand command, TContext context,
            TelegramChat chat, ITelegramBotData botData) {
            controller.MessageCommand = command;
            controller.TelegramContext = context;
            controller.TelegramChat = chat;
            controller.BotData = botData;
        }

        private void Dispatch(TController controller, Update update) {
            using (TContext context = new TContext()) {
                Console.WriteLine($"Received update - ID: {update.Id}");
                TelegramChat chat = null;
                if (update.Type == UpdateType.Message) {
                    var newChat = update.Message.Chat;
                    chat = TelegramChat.Get(context, newChat.Id);

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

                        if (chat.AllMembersAreAdministrators == newChat.AllMembersAreAdministrators) {
                            chat.AllMembersAreAdministrators = newChat.AllMembersAreAdministrators;
                        }
                    }
                    else {
                        chat = new TelegramChat(update.Message.Chat);
                        context.Add(chat);
                    }

                    Console.WriteLine(
                        $"Chat privata - Informazioni sull'utente: [{chat.Id}] @{chat.Username} State: {chat.State} - Role: {chat.Role}");
                }

                MessageCommand command;
                if (update.Type == UpdateType.Message) {
                    command = update.Message.GetCommand();
                    if (command.Target != null && command.Target != chat.Username) {
                        Console.WriteLine($"Command's target is @{command.Target} - Ignoring command");
                        return;
                    }
                }
                else {
                    command = new MessageCommand();
                }

                SetControllerData(controller, command, context, chat, _botData);

                foreach (var method in _methods.Where(m => m.GetCustomAttributes()
                                                            .Where(att => att is DispatcherFilterAttribute)
                                                            .All(attr =>
                                                                ((DispatcherFilterAttribute) attr).IsValid(update,
                                                                    chat, command, _botData))).ToArray()) {
                    /*
                    var parameters = method.GetParameters();
                    if (!parameters.Any() || parameters[0].ParameterType != typeof(Update)) {
                        throw new InvalidRouteMethodArguments(parameters?[0], "The first parameter must be the Update");
                    }

                    var arguments = new List<Object> {update};
                    foreach (var par in parameters.Skip(1)) {
                        if (par.ParameterType == typeof(MessageCommand)) {
                            arguments.Add(command);
                        }
                        else if (par.ParameterType == typeof(TContext)) {
                            arguments.Add(context);
                        }
                        else if (par.ParameterType == typeof(TelegramChat)) {
                            arguments.Add(chat);
                        }
                        else {
                            throw new InvalidRouteMethodArguments(par);
                        }
                    }
                    */
                    method.Invoke(controller, null);
                }

                try {
                    context.SaveChanges();
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private async Task DispatchAsync(TController controller, Update update)
        {
            using (TContext context = new TContext())
            {
                _logger.LogInformation($"Received update - ID: {update.Id}");
                TelegramChat chat = null;
                if (update.Type == UpdateType.Message)
                {
                    var newChat = update.Message.Chat;
                    chat = await TelegramChat.GetAsync(context, newChat.Id);

                    if (chat != null)
                    {
                        if (newChat.Username != null && chat.Username == newChat.Username)
                        {
                            chat.Username = newChat.Username;
                        }

                        if (newChat.Title != null && chat.Title == newChat.Title)
                        {
                            chat.Title = newChat.Title;
                        }

                        if (newChat.Description != null && chat.Description == newChat.Description)
                        {
                            chat.Description = newChat.Description;
                        }

                        if (newChat.InviteLink != null && chat.InviteLink == newChat.InviteLink)
                        {
                            chat.InviteLink = newChat.InviteLink;
                        }

                        if (newChat.LastName != null && chat.LastName == newChat.LastName)
                        {
                            chat.LastName = newChat.LastName;
                        }

                        if (newChat.StickerSetName != null && chat.StickerSetName == newChat.StickerSetName)
                        {
                            chat.StickerSetName = newChat.StickerSetName;
                        }

                        if (newChat.FirstName != null && chat.FirstName == newChat.FirstName)
                        {
                            chat.FirstName = newChat.FirstName;
                        }

                        if (newChat.CanSetStickerSet != null && chat.CanSetStickerSet == newChat.CanSetStickerSet)
                        {
                            chat.CanSetStickerSet = newChat.CanSetStickerSet;
                        }

                        if (chat.AllMembersAreAdministrators == newChat.AllMembersAreAdministrators)
                        {
                            chat.AllMembersAreAdministrators = newChat.AllMembersAreAdministrators;
                        }
                    }
                    else
                    {
                        chat = new TelegramChat(update.Message.Chat);
                        await context.AddAsync(chat);
                    }

                    _logger.LogInformation(
                        $"Private chat - User's informations: [{chat.Id}] @{chat.Username} State: {chat.State} - Role: {chat.Role}");
                }

                MessageCommand command;
                if (update.Type == UpdateType.Message)
                {
                    command = update.Message.GetCommand();
                    if (command.Target != null && command.Target != chat.Username)
                    {
                        _logger.LogDebug($"Command's target is @{command.Target} - Ignoring command");
                        return;
                    }
                }
                else {
                    command = new MessageCommand();
                }

                SetControllerData(controller, command, context, chat, _botData);
                _logger.LogDebug($"Command: {JsonConvert.SerializeObject(command, Formatting.Indented)}");
                _logger.LogDebug($"Chat: {JsonConvert.SerializeObject(chat, Formatting.Indented)}");

                bool executed = false;
                foreach (var method in _methods.Where(m => m.GetCustomAttributes()
                                                            .Where(att => att is DispatcherFilterAttribute)
                                                            .All(attr =>
                                                                ((DispatcherFilterAttribute)attr).IsValid(update,
                                                                    chat, command, _botData)))
                    .Where(m => 
                        (AsyncStateMachineAttribute) m.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
                    .ToArray())
                {
                    _logger.LogTrace($"Calling method: {method.Name}");
                    executed = true;
                    await (Task) method.Invoke(controller, null);
                }

                try
                {
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }

                if (!executed) {
                    _logger.LogInformation("No valid method found to manage current request.");
                }
            }
        }

        public void Dispose() {
            
        }
    }
}
