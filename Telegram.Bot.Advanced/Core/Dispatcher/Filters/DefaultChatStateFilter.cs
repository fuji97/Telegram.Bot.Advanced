using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters;

/// <summary>
/// The method is eligible if the chat has no state assigned (i.e. its state is null).
/// </summary>
public sealed class DefaultChatStateFilter : DispatcherFilterAttribute {
    /// <inheritdoc />
    public override bool IsValid(Update update, TelegramChat? chat, MessageCommand command, ITelegramBotData botData) => chat?.State is null;
}
