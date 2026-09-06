namespace Telegram.Bot.Advanced.Exceptions;

/// <summary>
/// Throws when trying to add a data in TelegramChat that have the same key of an existing one
/// </summary>
public sealed class DuplicateDataKeyException : TelegramBotAdvancedException {
    public DuplicateDataKeyException() {
    }

    public DuplicateDataKeyException(string message) : base(message) {
    }

    public DuplicateDataKeyException(string message, Exception inner) : base(message, inner) {
    }
}
