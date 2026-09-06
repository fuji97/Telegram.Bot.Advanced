using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Controller;

public sealed class NewsletterPublishingController<TContext> : TelegramController<TContext> where TContext : TelegramContext {
    private const string SendingNewsletterState = "TBA_sendingNewsletter";
    private const string NewsletterDataKey = "newsletter";

    private readonly INewsletterService _newsletterService;

    public NewsletterPublishingController(INewsletterService newsletterService) {
        _newsletterService = newsletterService;
    }

    [ChatRoleFilter(ChatRole.Administrator, ChatRole.Moderator), CommandFilter("send_newsletter")]
    public async Task SendNewsletter() {
        if (MessageCommand.Parameters.Count != 1) {
            await ReplyTextMessageAsync("Usage:\n/send_newsletter <newsletter>", cancellationToken: CancellationToken);
            return;
        }

        var newsletterKey = MessageCommand.Parameters[0];
        var newsletter = await _newsletterService.GetNewsletterByKeyAsync(newsletterKey, CancellationToken);

        if (newsletter is null) {
            await ReplyTextMessageAsync(
                $"The newsletter {newsletterKey} doesn't exist.\n" +
                $"Use /create_newsletter {newsletterKey} - to create it", cancellationToken: CancellationToken);
            return;
        }

        TelegramChat!.State = SendingNewsletterState;
        TelegramChat[NewsletterDataKey] = newsletterKey;
        await ReplyTextMessageAsync("Ok, now send me the text formatted as HTML", cancellationToken: CancellationToken);

        await TelegramContext.SaveChangesAsync(CancellationToken);
    }

    [ChatRoleFilter(ChatRole.Administrator, ChatRole.Moderator), CommandFilter("send_global_newsletter")]
    public async Task SendGlobalNewsletter() {
        if (MessageCommand.Parameters.Count != 0) {
            await ReplyTextMessageAsync("Usage:\n/send_global_newsletter", cancellationToken: CancellationToken);
            return;
        }

        TelegramChat!.State = SendingNewsletterState;
        TelegramChat[NewsletterDataKey] = null;
        await ReplyTextMessageAsync("Ok, now send me the text formatted as HTML", cancellationToken: CancellationToken);

        await TelegramContext.SaveChangesAsync(CancellationToken);
    }

    [ChatRoleFilter(ChatRole.Administrator, ChatRole.Moderator), ChatStateFilter(SendingNewsletterState),
     UpdateTypeFilter(UpdateType.Message), NoCommandFilter]
    public async Task SendNewsletterGetText() {
        var newsletterKey = TelegramChat![NewsletterDataKey];

        if (newsletterKey is not null && await _newsletterService.GetNewsletterByKeyAsync(newsletterKey, CancellationToken) is null) {
            await ReplyTextMessageAsync(
                $"The newsletter {newsletterKey} was deleted before the content was received. Aborting.",
                cancellationToken: CancellationToken);
            TelegramChat.State = null;
            TelegramChat[NewsletterDataKey] = null;
            await TelegramContext.SaveChangesAsync(CancellationToken);
            return;
        }

        var message = Update.Message!;
        var sendFunction = SelectSendFunction(message);
        if (sendFunction is null) {
            await ReplyTextMessageAsync(
                "Only text, photo, audio or sticker is supported as a type of message, please send one of these type",
                cancellationToken: CancellationToken);
            return;
        }

        await ReplyTextMessageAsync(
            newsletterKey is not null ? "Ok, sending the newsletter..." : "Ok, sending the global newsletter...",
            cancellationToken: CancellationToken);

        var result = newsletterKey is not null
            ? await _newsletterService.SendNewsletterAsync(newsletterKey, sendFunction, CancellationToken)
            : await _newsletterService.SendNewsletterAsync(sendFunction, CancellationToken);

        await ReplyTextMessageAsync("Finished - report:\n" +
                                    $"Successful delivery: {result.TotalSuccesses}\n" +
                                    $"Failed delivery: {result.TotalErrors}", cancellationToken: CancellationToken);
        TelegramChat.State = null;
        await TelegramContext.SaveChangesAsync(CancellationToken);
    }

    private Func<TelegramChat, CancellationToken, Task>? SelectSendFunction(Message message) => message.Type switch {
        MessageType.Text => (chat, cancellationToken) =>
            BotData.Bot.SendMessage(chat.Id, message.Text!, ParseMode.Html, cancellationToken: cancellationToken),
        MessageType.Photo => (chat, cancellationToken) =>
            BotData.Bot.SendPhoto(chat.Id, message.Photo![^1].FileId, message.Caption, ParseMode.Html,
                captionEntities: message.CaptionEntities, cancellationToken: cancellationToken),
        MessageType.Audio => (chat, cancellationToken) =>
            BotData.Bot.SendAudio(chat.Id, message.Audio!.FileId, message.Caption, ParseMode.Html,
                captionEntities: message.CaptionEntities, cancellationToken: cancellationToken),
        MessageType.Sticker => (chat, cancellationToken) =>
            BotData.Bot.SendSticker(chat.Id, message.Sticker!.FileId, cancellationToken: cancellationToken),
        _ => null
    };
}
