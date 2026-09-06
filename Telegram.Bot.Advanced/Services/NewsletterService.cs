using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Services;

public sealed class NewsletterService<TDbContext> : INewsletterService where TDbContext : TelegramContext {
    private readonly TDbContext _context;
    private readonly ILogger<NewsletterService<TDbContext>> _logger;

    public NewsletterService(TDbContext context, ILogger<NewsletterService<TDbContext>> logger) {
        _context = context;
        _logger = logger;
    }

    private IQueryable<TelegramChat> GetChatFromNewsletterKey(string newsletterKey) =>
        _context.NewsletterChats
            .Include(nc => nc.Chat)
            .Where(nc => nc.NewsletterKey == newsletterKey)
            .Select(nc => nc.Chat);

    private async Task<Newsletter> GetRequiredNewsletterAsync(string newsletterKey, CancellationToken cancellationToken) =>
        await _context.Newsletters.FindAsync([newsletterKey], cancellationToken)
        ?? throw new NewsletterException($"The newsletter '{newsletterKey}' doesn't exist");

    private async Task<TelegramChat> GetRequiredChatAsync(long chatId, CancellationToken cancellationToken) =>
        await _context.Users.FindAsync([chatId], cancellationToken)
        ?? throw new NewsletterException($"The chat '{chatId}' doesn't exist");

    private static async Task<SendResult> SendToChatsAsync(List<TelegramChat> chats,
        Func<TelegramChat, CancellationToken, Task> sendAction, CancellationToken cancellationToken) {
        var successes = 0;
        Dictionary<TelegramChat, Exception> errors = [];

        foreach (var chat in chats) {
            cancellationToken.ThrowIfCancellationRequested();

            try {
                await sendAction(chat, cancellationToken);
                successes++;
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                errors.Add(chat, e);
            }
        }

        return new SendResult(successes, errors);
    }

    /// <inheritdoc />
    public async Task<SendResult> SendNewsletterAsync(string newsletterKey, Func<TelegramChat, CancellationToken, Task> sendAction,
        CancellationToken cancellationToken = default) {
        var chats = await GetChatFromNewsletterKey(newsletterKey).ToListAsync(cancellationToken);
        return await SendToChatsAsync(chats, sendAction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SendResult> SendNewsletterAsync(Func<TelegramChat, CancellationToken, Task> sendAction,
        CancellationToken cancellationToken = default) {
        var chats = await _context.Users.ToListAsync(cancellationToken);
        return await SendToChatsAsync(chats, sendAction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SubscribeChatAsync(string newsletterKey, long chatId, CancellationToken cancellationToken = default) {
        var newsletter = await GetRequiredNewsletterAsync(newsletterKey, cancellationToken);
        var chat = await GetRequiredChatAsync(chatId, cancellationToken);

        if (await _context.NewsletterChats.FindAsync([newsletterKey, chatId], cancellationToken) != null) {
            return false;
        }

        await _context.NewsletterChats.AddAsync(new NewsletterChat(newsletter, chat), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> UnsubscribeChatAsync(string newsletterKey, long chatId, CancellationToken cancellationToken = default) {
        await GetRequiredNewsletterAsync(newsletterKey, cancellationToken);
        await GetRequiredChatAsync(chatId, cancellationToken);

        var newsletterChat = await _context.NewsletterChats.FindAsync([newsletterKey, chatId], cancellationToken);
        if (newsletterChat == null) return false;

        _context.NewsletterChats.Remove(newsletterChat);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsChatSubscribedToNewsletterAsync(string newsletterKey, long chatId, CancellationToken cancellationToken = default) {
        await GetRequiredNewsletterAsync(newsletterKey, cancellationToken);
        await GetRequiredChatAsync(chatId, cancellationToken);

        return await _context.NewsletterChats.FindAsync([newsletterKey, chatId], cancellationToken) != null;
    }

    /// <inheritdoc />
    public async Task<Newsletter?> GetNewsletterByKeyAsync(string newsletterKey, CancellationToken cancellationToken = default) =>
        await _context.Newsletters.FindAsync([newsletterKey], cancellationToken);

    /// <inheritdoc />
    public async Task<List<Newsletter>> GetNewslettersAsync(CancellationToken cancellationToken = default) =>
        await _context.Newsletters.ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> CreateNewsletterAsync(Newsletter newsletter, CancellationToken cancellationToken = default) {
        if (await _context.Newsletters.AnyAsync(n => n.Key == newsletter.Key, cancellationToken)) {
            return false;
        }

        await _context.Newsletters.AddAsync(newsletter, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveNewsletterAsync(string newsletterKey, CancellationToken cancellationToken = default) {
        var newsletter = await _context.Newsletters.FindAsync([newsletterKey], cancellationToken);
        if (newsletter == null) return false;

        _context.Newsletters.Remove(newsletter);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public static class NewsletterServiceExtensions {
    public static IServiceCollection AddNewsletter<TDbContext>(this IServiceCollection services) where TDbContext : TelegramContext {
        services.AddScoped<INewsletterService, NewsletterService<TDbContext>>();
        return services;
    }
}
