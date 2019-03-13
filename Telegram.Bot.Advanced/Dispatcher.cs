using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.DispatcherFilters;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced
{
    public class Dispatcher<TContext, TController> : IDisposable, IDispatcher
        where TContext : TelegramContext, new() 
        where TController : class, ITelegramController<TContext>, new() {
        private readonly IEnumerable<MethodInfo> _methods;
        private readonly ILogger _logger;
        private IServiceProvider provider;
        //private IServiceCollection services;
        //private IServiceScope scope;

        public Dispatcher(IServiceProvider provider = null) {
            this.provider = provider;

            _methods = typeof(TController).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                 .Where(m => m.GetCustomAttributes(typeof(DispatcherFilterAttribute), false).Length > 0);

            ILoggerFactory loggerFactory = new LoggerFactory();
            _logger = loggerFactory.CreateLogger<Dispatcher<TContext, TController>>();
        }

        public void DispatchUpdate(Update update) {
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

        public async Task DispatchUpdateAsync(Update update)
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

        private TController SetControllerData(TController controller, MessageCommand command, TContext context,
            TelegramChat chat) {
            controller.MessageCommand = command;
            controller.TelegramContext = context;
            controller.TelegramChat = chat;
            return controller;
        }

        private void Dispatch(TController controller, Update update) {
            using (TContext context = new TContext()) {
                Console.WriteLine($"Received update - ID: {update.Id}");
                TelegramChat chat = null;
                if (update.Type == UpdateType.Message) {
                    var newChat = update.Message.Chat;
                    chat = TelegramChat.Get(context, newChat.Id);

                    if (chat != null) {
                        var updated = false;
                        if (newChat.Username != null && chat.Username == newChat.Username) {
                            chat.Username = newChat.Username;
                            updated = true;
                        }

                        if (newChat.Title != null && chat.Title == newChat.Title) {
                            chat.Title = newChat.Title;
                            updated = true;
                        }

                        if (newChat.Description != null && chat.Description == newChat.Description) {
                            chat.Description = newChat.Description;
                            updated = true;
                        }

                        if (newChat.InviteLink != null && chat.InviteLink == newChat.InviteLink) {
                            chat.InviteLink = newChat.InviteLink;
                            updated = true;
                        }

                        if (newChat.LastName != null && chat.LastName == newChat.LastName) {
                            chat.LastName = newChat.LastName;
                            updated = true;
                        }

                        if (newChat.StickerSetName != null && chat.StickerSetName == newChat.StickerSetName) {
                            chat.StickerSetName = newChat.StickerSetName;
                            updated = true;
                        }

                        if (newChat.FirstName != null && chat.FirstName == newChat.FirstName) {
                            chat.FirstName = newChat.FirstName;
                            updated = true;
                        }

                        if (newChat.CanSetStickerSet != null && chat.CanSetStickerSet == newChat.CanSetStickerSet) {
                            chat.CanSetStickerSet = newChat.CanSetStickerSet;
                            updated = true;
                        }

                        if (chat.AllMembersAreAdministrators == newChat.AllMembersAreAdministrators) {
                            chat.AllMembersAreAdministrators = newChat.AllMembersAreAdministrators;
                            updated = true;
                        }

                        if (updated)
                            chat.Update(context);
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

                SetControllerData(controller, command, context, chat);

                foreach (var method in _methods.Where(m => m.GetCustomAttributes()
                                                            .Where(att => att is DispatcherFilterAttribute)
                                                            .All(attr =>
                                                                ((DispatcherFilterAttribute) attr).IsValid(update,
                                                                    chat, command))).ToArray()) {
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
                Console.WriteLine($"Received update - ID: {update.Id}");
                TelegramChat chat = null;
                if (update.Type == UpdateType.Message)
                {
                    var newChat = update.Message.Chat;
                    chat = await TelegramChat.GetAsync(context, newChat.Id);

                    if (chat != null)
                    {
                        var updated = false;
                        if (newChat.Username != null && chat.Username == newChat.Username)
                        {
                            chat.Username = newChat.Username;
                            updated = true;
                        }

                        if (newChat.Title != null && chat.Title == newChat.Title)
                        {
                            chat.Title = newChat.Title;
                            updated = true;
                        }

                        if (newChat.Description != null && chat.Description == newChat.Description)
                        {
                            chat.Description = newChat.Description;
                            updated = true;
                        }

                        if (newChat.InviteLink != null && chat.InviteLink == newChat.InviteLink)
                        {
                            chat.InviteLink = newChat.InviteLink;
                            updated = true;
                        }

                        if (newChat.LastName != null && chat.LastName == newChat.LastName)
                        {
                            chat.LastName = newChat.LastName;
                            updated = true;
                        }

                        if (newChat.StickerSetName != null && chat.StickerSetName == newChat.StickerSetName)
                        {
                            chat.StickerSetName = newChat.StickerSetName;
                            updated = true;
                        }

                        if (newChat.FirstName != null && chat.FirstName == newChat.FirstName)
                        {
                            chat.FirstName = newChat.FirstName;
                            updated = true;
                        }

                        if (newChat.CanSetStickerSet != null && chat.CanSetStickerSet == newChat.CanSetStickerSet)
                        {
                            chat.CanSetStickerSet = newChat.CanSetStickerSet;
                            updated = true;
                        }

                        if (chat.AllMembersAreAdministrators == newChat.AllMembersAreAdministrators)
                        {
                            chat.AllMembersAreAdministrators = newChat.AllMembersAreAdministrators;
                            updated = true;
                        }

                        if (updated)
                            chat.Update(context);
                    }
                    else
                    {
                        chat = new TelegramChat(update.Message.Chat);
                        await context.AddAsync(chat);
                    }

                    Console.WriteLine(
                        $"Chat privata - Informazioni sull'utente: [{chat.Id}] @{chat.Username} State: {chat.State} - Role: {chat.Role}");
                }

                MessageCommand command;
                if (update.Type == UpdateType.Message)
                {
                    command = update.Message.GetCommand();
                    if (command.Target != null && command.Target != chat.Username)
                    {
                        Console.WriteLine($"Command's target is @{command.Target} - Ignoring command");
                        return;
                    }
                }
                else
                {
                    command = new MessageCommand();
                }

                SetControllerData(controller, command, context, chat);

                foreach (var method in _methods.Where(m => m.GetCustomAttributes()
                                                            .Where(att => att is DispatcherFilterAttribute)
                                                            .All(attr =>
                                                                ((DispatcherFilterAttribute)attr).IsValid(update,
                                                                    chat, command)))
                    .Where(m => 
                        ((AsyncStateMachineAttribute) m.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null))
                    .ToArray())
                {
                    /*
                    var parameters = method.GetParameters();
                    if (!parameters.Any() || parameters[0].ParameterType != typeof(Update))
                    {
                        throw new InvalidRouteMethodArguments(parameters?[0], "The first parameter must be the Update");
                    }

                    var arguments = new List<Object> { update };
                    foreach (var par in parameters.Skip(1))
                    {
                        if (par.ParameterType == typeof(MessageCommand))
                        {
                            arguments.Add(command);
                        }
                        else if (par.ParameterType == typeof(TContext))
                        {
                            arguments.Add(context);
                        }
                        else if (par.ParameterType == typeof(TelegramChat))
                        {
                            arguments.Add(chat);
                        }
                        else
                        {
                            throw new InvalidRouteMethodArguments(par);
                        }
                    }
                    */

                    await (Task) method.Invoke(controller, null);
                }

                try
                {
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void Dispose() {
            
        }
    }

    public static class Dispatcher {
        public static IServiceCollection AddTelegramDispatcher<TContext, TController>(this IServiceCollection services) 
            where TContext : TelegramContext, new() 
            where TController : class, ITelegramController<TContext>, new() {
            services.AddSingleton<Dispatcher<TContext, TController>>();
            services.AddScoped<TController>();
            return services;
        }
        
        //public async static Task<IActionResult> 
    }
}
