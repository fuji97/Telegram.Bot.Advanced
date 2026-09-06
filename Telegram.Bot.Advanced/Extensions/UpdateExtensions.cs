using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Extensions;

public static class UpdateExtensions {
    /// <summary>
    /// Returns the message carried by the update, covering every update kind that carries one.
    /// Returns null for update kinds without a message (e.g. an inline-mode callback query).
    /// </summary>
    public static Message? GetMessage(this Update update) => update switch {
        { Message: { } message } => message,
        { EditedMessage: { } message } => message,
        { ChannelPost: { } message } => message,
        { EditedChannelPost: { } message } => message,
        { BusinessMessage: { } message } => message,
        { EditedBusinessMessage: { } message } => message,
        { CallbackQuery.Message: { } message } => message,
        _ => null
    };
}
