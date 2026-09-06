using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Core.Dispatcher;

/// <summary>
/// The dispatcher class that receives the Update, resolves the correct handler and invokes it
/// </summary>
/// <typeparam name="TContext">Context used by the application</typeparam>
public sealed class Dispatcher<TContext> : IDispatcher
    where TContext : TelegramContext {

    private static readonly JsonSerializerOptions TraceSerializerOptions = new() { WriteIndented = true };

    private readonly ITelegramBotData _botData;
    private readonly IList<Type> _controllers;
    private readonly HandlerDescriptor[] _handlers;
    private readonly HandlerDescriptor? _fallback;

    public Dispatcher(ITelegramBotData botData, IList<Type> controllers) {
        ArgumentNullException.ThrowIfNull(botData);
        ArgumentNullException.ThrowIfNull(controllers);

        ValidateControllers(controllers);

        _botData = botData;
        _controllers = controllers;
        _handlers = BuildHandlers(controllers);

        var fallbacks = _handlers.Where(h => h.IsFallback).ToArray();
        if (fallbacks.Length > 1) {
            throw new InvalidControllerException(
                "Only one handler method may be marked with [NoMethodFilter].");
        }

        _fallback = fallbacks.Length == 1 ? fallbacks[0] : null;
    }

    private static void ValidateControllers(IList<Type> controllers) {
        HashSet<Type> seen = [];
        foreach (var controller in controllers) {
            if (!seen.Add(controller)) {
                throw new InvalidControllerException(
                    $"{controller.FullName} is registered more than once as a controller.");
            }

            if (controller.IsGenericTypeDefinition) {
                throw new InvalidControllerException(
                    $"{controller.FullName} is an open generic type and cannot be used as a controller.");
            }

            if (!IsControllerType(controller)) {
                throw new InvalidControllerException(
                    $"{controller.FullName} is not a valid controller. Make sure that the controller is " +
                    $"an instantiable class that implements ITelegramController<{typeof(TContext).Name}>");
            }
        }
    }

    private static bool IsControllerType(Type controller) {
        return controller.IsClass &&
               !controller.IsAbstract &&
               controller.GetInterfaces().Contains(typeof(ITelegramController<TContext>));
    }

    private static HandlerDescriptor[] BuildHandlers(IList<Type> controllers) {
        List<HandlerDescriptor> descriptors = [];

        foreach (var controllerType in controllers) {
            var controllerFilters = controllerType.GetCustomAttributes<DispatcherFilterAttribute>().ToArray();
            var methods = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .OrderBy(m => m.MetadataToken);

            foreach (var method in methods) {
                if (method.GetParameters().Length != 0) {
                    throw new InvalidControllerException(
                        $"Handler method '{controllerType.FullName}.{method.Name}' must not declare parameters.");
                }

                var returnKind = GetReturnKind(controllerType, method);
                var methodFilters = method.GetCustomAttributes<DispatcherFilterAttribute>().ToArray();
                var isFallback = method.GetCustomAttribute<NoMethodFilter>() != null;

                descriptors.Add(new HandlerDescriptor(controllerType, method, controllerFilters, methodFilters,
                    returnKind, isFallback));
            }
        }

        return descriptors.ToArray();
    }

    private static HandlerReturnKind GetReturnKind(Type controllerType, MethodInfo method) {
        if (method.ReturnType == typeof(void)) return HandlerReturnKind.Void;
        if (method.ReturnType == typeof(Task)) return HandlerReturnKind.Task;
        if (method.ReturnType == typeof(ValueTask)) return HandlerReturnKind.ValueTask;

        throw new InvalidControllerException(
            $"Handler method '{controllerType.FullName}.{method.Name}' must return void, Task, or ValueTask.");
    }

    public IList<Type> GetControllersType() => _controllers;

    public Type GetContextType() => typeof(TContext);

    public void RegisterController(IServiceCollection services) {
        foreach (var controller in _controllers) {
            services.TryAddScoped(controller);
        }
    }

    public async Task DispatchUpdateAsync(Update update, IServiceProvider provider, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(provider);

        await using var scope = provider.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetService<ILogger<Dispatcher<TContext>>>();

        await DispatchAsync(scope.ServiceProvider, update, logger, cancellationToken);
    }

    public async Task HandleErrorAsync(Exception exception, IServiceProvider provider, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(provider);

        await using var scope = provider.CreateAsyncScope();
        cancellationToken.ThrowIfCancellationRequested();

        var logger = scope.ServiceProvider.GetService<ILogger<Dispatcher<TContext>>>();
        logger?.LogError(exception, "Telegram APIs raised an error");
    }

    private async Task DispatchAsync(IServiceProvider serviceProvider, Update update, ILogger? logger, CancellationToken cancellationToken) {
        var context = serviceProvider.GetRequiredService<TContext>();
        logger?.LogInformation("Received update - ID: {Id}", update.Id);

        MessageCommand command;
        if (update.Type == UpdateType.Message) {
            var message = update.Message!;
            command = new MessageCommand(message);

            if (command.Target != null &&
                !string.Equals(command.Target, _botData.Username, StringComparison.OrdinalIgnoreCase)) {
                logger?.LogInformation("Ignoring command targeted at another bot: {Target}", command.Target);
                return;
            }

            var ignoreBehaviour = message.Chat.IsGroup() ? _botData.GroupChatBehaviour : _botData.PrivateChatBehaviour;
            switch (ignoreBehaviour) {
                case IgnoreBehaviour.IgnoreAllMessages:
                    return;
                case IgnoreBehaviour.IgnoreNonCommandMessages:
                    if (!command.IsCommand()) return;
                    break;
                case IgnoreBehaviour.IgnoreAllMessagesAndCommandsWithoutTarget:
                    if (command.Target == null) return;
                    break;
                case IgnoreBehaviour.IgnoreNothing:
                    break;
            }
        }
        else {
            command = new MessageCommand();
        }

        var chat = await UpdateChat(update, context, cancellationToken);

        switch (_botData.UserUpdate) {
            case UserUpdate.BotCommand:
                if (command.IsCommand()) {
                    await UpdateUser(update, context, chat, cancellationToken);
                }
                break;
            case UserUpdate.EveryMessage:
                await UpdateUser(update, context, chat, cancellationToken);
                break;
        }

        await context.SaveChangesAsync(cancellationToken);

        HandlerDescriptor? handler;
        try {
            handler = SelectHandler(update, chat, command);
        }
        catch (Exception e) {
            logger?.LogError(e, "An exception was thrown while filtering the eligible handlers");
            throw;
        }

        if (handler is null) {
            if (_fallback is not null && _fallback.IsEligible(update, chat, command, _botData)) {
                handler = _fallback;
            }
            else {
                logger?.LogInformation("No valid method found to handle the current request");
                return;
            }
        }

        if (logger != null && logger.IsEnabled(LogLevel.Trace)) {
            logger.LogTrace("Command: {Command}", JsonSerializer.Serialize(command, TraceSerializerOptions));
            logger.LogTrace("Chat: {Chat}", JsonSerializer.Serialize(chat, TraceSerializerOptions));
        }

        try {
            await ExecuteHandler(handler, serviceProvider, update, command, context, chat, logger, cancellationToken);
        }
        catch (Exception e) {
            logger?.LogError(e, "An exception was thrown while dispatching the request");
            throw;
        }

        logger?.LogTrace("End of dispatching");
    }

    private HandlerDescriptor? SelectHandler(Update update, TelegramChat? chat, MessageCommand command) {
        foreach (var descriptor in _handlers) {
            if (descriptor.IsFallback) continue;
            if (descriptor.IsEligible(update, chat, command, _botData)) {
                return descriptor;
            }
        }

        return null;
    }

    private async Task ExecuteHandler(HandlerDescriptor handler, IServiceProvider serviceProvider, Update update, MessageCommand command,
        TContext context, TelegramChat? chat, ILogger? logger, CancellationToken cancellationToken) {
        var controller = (ITelegramController<TContext>) serviceProvider.GetRequiredService(handler.ControllerType);
        SetControllerData(controller, update, command, context, chat, _botData, cancellationToken);

        logger?.LogInformation("Calling handler method: {Name}", handler.Method.Name);

        object? result;
        try {
            result = handler.Method.Invoke(controller, null);
        }
        catch (TargetInvocationException ex) {
            ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            throw;
        }

        switch (handler.ReturnKind) {
            case HandlerReturnKind.Task:
                await (Task) result!;
                break;
            case HandlerReturnKind.ValueTask:
                await (ValueTask) result!;
                break;
        }
    }

    private static void SetControllerData(ITelegramController<TContext> controller, Update update, MessageCommand command,
        TContext context, TelegramChat? chat, ITelegramBotData botData, CancellationToken cancellationToken) {
        controller.Update = update;
        controller.MessageCommand = command;
        controller.TelegramContext = context;
        controller.TelegramChat = chat;
        controller.BotData = botData;
        controller.CancellationToken = cancellationToken;
    }

    private async Task<TelegramChat?> UpdateChat(Update update, TContext context, CancellationToken cancellationToken) {
        var message = update.GetMessage();
        if (message?.Chat == null) {
            return null;
        }

        var newChat = message.Chat;
        var chat = await TelegramChat.GetAsync(context, newChat.Id, cancellationToken);

        if (chat != null) {
            if (newChat.Username != null) chat.Username = newChat.Username;
            if (newChat.Title != null) chat.Title = newChat.Title;
            if (newChat.LastName != null) chat.LastName = newChat.LastName;
            if (newChat.FirstName != null) chat.FirstName = newChat.FirstName;
        }
        else {
            chat = new TelegramChat(newChat);

            var chatFullInfo = await _botData.Bot.GetChat(newChat.Id, cancellationToken);
            chat.Description = chatFullInfo.Description;
            chat.InviteLink = chatFullInfo.InviteLink;
            chat.StickerSetName = chatFullInfo.StickerSetName;
            chat.CanSetStickerSet = chatFullInfo.CanSetStickerSet;

            var defaultRole = _botData.DefaultUserRole.FirstOrDefault(d => d.Equals(chat));
            if (defaultRole != null) {
                chat.Role = defaultRole.Role;
            }

            await context.AddAsync(chat, cancellationToken);
        }

        return chat;
    }

    private static async Task UpdateUser(Update update, TContext context, TelegramChat? currentChat, CancellationToken cancellationToken) {
        var updateUser = update.GetMessage()?.From;

        if (updateUser == null || updateUser.Id == currentChat?.Id) {
            return;
        }

        var storedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == updateUser.Id, cancellationToken);
        if (storedUser != null) {
            storedUser.FirstName = updateUser.FirstName;
            if (updateUser.Username != null) storedUser.Username = updateUser.Username;
            if (updateUser.LastName != null) storedUser.LastName = updateUser.LastName;
        }
        else {
            var newUserChat = updateUser.ToModel();
            await context.AddAsync(newUserChat, cancellationToken);
        }
    }
}
