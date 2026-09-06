using System.Reflection;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Dispatcher;

/// <summary>
/// The kind of value returned by a handler method, as accepted by the dispatcher.
/// </summary>
internal enum HandlerReturnKind {
    Void,
    Task,
    ValueTask
}

/// <summary>
/// Immutable description of a single handler method, built once when the Dispatcher is constructed.
/// </summary>
internal sealed record HandlerDescriptor(
    Type ControllerType,
    MethodInfo Method,
    IReadOnlyList<DispatcherFilterAttribute> ControllerFilters,
    IReadOnlyList<DispatcherFilterAttribute> MethodFilters,
    HandlerReturnKind ReturnKind,
    bool IsFallback) {

    /// <summary>
    /// True if every controller-level and method-level filter accepts the current update.
    /// </summary>
    public bool IsEligible(Update update, TelegramChat? chat, MessageCommand command, ITelegramBotData botData) {
        foreach (var filter in ControllerFilters) {
            if (!filter.IsValidController(update, chat, command, botData)) {
                return false;
            }
        }

        foreach (var filter in MethodFilters) {
            if (!filter.IsValidMethod(update, chat, command, botData)) {
                return false;
            }
        }

        return true;
    }
}
