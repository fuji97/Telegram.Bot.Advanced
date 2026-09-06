using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Services;

public interface INewsletterService {
    /// <summary>
    /// Execute sendAction sequentially for each chat subscribed to newsletterKey. Delivery stops propagating
    /// cancellation immediately if cancellationToken is triggered; any other exception thrown by sendAction for
    /// a given chat is recorded in the returned result instead of aborting the whole newsletter.
    /// </summary>
    Task<SendResult> SendNewsletterAsync(string newsletterKey, Func<TelegramChat, CancellationToken, Task> sendAction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute sendAction sequentially for every chat known to the bot. Delivery stops propagating cancellation
    /// immediately if cancellationToken is triggered; any other exception thrown by sendAction for a given chat
    /// is recorded in the returned result instead of aborting the whole newsletter.
    /// </summary>
    Task<SendResult> SendNewsletterAsync(Func<TelegramChat, CancellationToken, Task> sendAction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe the chat to the newsletter. Returns false if the chat is already subscribed.
    /// </summary>
    Task<bool> SubscribeChatAsync(string newsletterKey, long chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribe the chat from the newsletter. Returns false if the chat wasn't subscribed.
    /// </summary>
    Task<bool> UnsubscribeChatAsync(string newsletterKey, long chatId, CancellationToken cancellationToken = default);

    Task<bool> IsChatSubscribedToNewsletterAsync(string newsletterKey, long chatId, CancellationToken cancellationToken = default);

    Task<List<Newsletter>> GetNewslettersAsync(CancellationToken cancellationToken = default);

    Task<Newsletter?> GetNewsletterByKeyAsync(string newsletterKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create the newsletter. Returns false if a newsletter with the same key already exists.
    /// </summary>
    Task<bool> CreateNewsletterAsync(Newsletter newsletter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove the newsletter. Returns false if no newsletter with that key exists.
    /// </summary>
    Task<bool> RemoveNewsletterAsync(string newsletterKey, CancellationToken cancellationToken = default);
}
