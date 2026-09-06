using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters;

/// <summary>
/// The method is eligible if the state of the chat matches with one of the passed states.
/// </summary>
public sealed class ChatStateFilter : DispatcherFilterAttribute {
    private readonly string[] _state;

    public ChatStateFilter(params string[] state) {
        _state = state;
    }

    /// <inheritdoc />
    public override bool IsValid(Update update, TelegramChat? user, MessageCommand command, ITelegramBotData botData) => user != null && _state.Contains(user.State);
}
