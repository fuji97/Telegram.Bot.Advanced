using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Controller;

public sealed class NewsletterSubscriptionController<TContext> : TelegramController<TContext> where TContext : TelegramContext {
    private readonly INewsletterService _newsletterService;

    public NewsletterSubscriptionController(INewsletterService newsletterService) {
        _newsletterService = newsletterService;
    }

    [CommandFilter("subscribe")]
    public async Task SubscribeToNewsletter() {
        if (!await CheckIfAdminInGroups()) return;

        if (MessageCommand.Parameters.Count != 1) {
            await ReplyTextMessageAsync("Usage:\n/subscribe <newsletter>", cancellationToken: CancellationToken);
            return;
        }
        var newsletterKey = MessageCommand.Parameters[0];

        var newsletter = await _newsletterService.GetNewsletterByKeyAsync(newsletterKey, CancellationToken);
        if (newsletter is null) {
            await ReplyTextMessageAsync($"The newsletter {newsletterKey} doesn't exist.", cancellationToken: CancellationToken);
            return;
        }

        var subscribed = await _newsletterService.SubscribeChatAsync(newsletterKey, TelegramChat!.Id, CancellationToken);
        await ReplyTextMessageAsync(subscribed
            ? $"Successfully subscribed to the {newsletterKey} newsletter"
            : "Can't subscribe to newsletter, probably you are already subscribed", cancellationToken: CancellationToken);
    }

    [CommandFilter("unsubscribe")]
    public async Task UnsubscribeFromNewsletter() {
        if (!await CheckIfAdminInGroups()) return;

        if (MessageCommand.Parameters.Count != 1) {
            await ReplyTextMessageAsync("Usage:\n/unsubscribe <newsletter>", cancellationToken: CancellationToken);
            return;
        }
        var newsletterKey = MessageCommand.Parameters[0];

        var newsletter = await _newsletterService.GetNewsletterByKeyAsync(newsletterKey, CancellationToken);
        if (newsletter is null) {
            await ReplyTextMessageAsync($"The newsletter {newsletterKey} doesn't exist.", cancellationToken: CancellationToken);
            return;
        }

        var unsubscribed = await _newsletterService.UnsubscribeChatAsync(newsletterKey, TelegramChat!.Id, CancellationToken);
        await ReplyTextMessageAsync(unsubscribed
            ? $"Successfully unsubscribed from the {newsletterKey} newsletter"
            : "You are not subscribed to this newsletter", cancellationToken: CancellationToken);
    }

    private async Task<bool> CheckIfAdminInGroups() {
        if (TelegramChat is { Type: ChatType.Supergroup or ChatType.Group }) {
            var fromId = Update.Message?.From?.Id;
            if (fromId is null) return false;

            var administrators = await BotData.Bot.GetChatAdministrators(TelegramChat.Id, cancellationToken: CancellationToken);
            if (administrators.All(a => a.User.Id != fromId)) {
                // Do not answer, just to avoid bot spam
                return false;
            }
        }

        return true;
    }
}
