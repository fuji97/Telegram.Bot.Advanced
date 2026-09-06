using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Models;

/// <summary>
/// Immutable outcome of a newsletter send operation.
/// </summary>
public sealed class SendResult {
    public int TotalSuccesses { get; }
    public IReadOnlyDictionary<TelegramChat, Exception> Errors { get; }

    public int TotalErrors => Errors.Count;
    public int TotalSubscribers => TotalSuccesses + TotalErrors;

    public SendResult(int totalSuccesses, IReadOnlyDictionary<TelegramChat, Exception> errors) {
        TotalSuccesses = totalSuccesses;
        Errors = new Dictionary<TelegramChat, Exception>(errors);
    }
}
