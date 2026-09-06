using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Advanced.Controller;

/// <summary>
/// Base implementation of ITelegramContext
/// </summary>
/// <typeparam name="TContext">Telegram context used by the application</typeparam>
public class TelegramController<TContext> : ITelegramController<TContext> where TContext : TelegramContext {
    public MessageCommand MessageCommand { get; set; } = null!;
    public TContext TelegramContext { get; set; } = null!;
    public TelegramChat? TelegramChat { get; set; } = null!;
    public ITelegramBotData BotData { get; set; } = null!;
    public Update Update { get; set; } = null!;

    private CancellationToken _cancellationToken;

    CancellationToken ITelegramController<TContext>.CancellationToken {
        set => _cancellationToken = value;
    }

    /// <summary>
    /// Token used to cancel Telegram/EF calls performed while handling the current update
    /// </summary>
    protected CancellationToken CancellationToken => _cancellationToken;

    /// <summary>
    /// Shortcut to send a message to current chat
    /// </summary>
    /// <param name="text"></param>
    /// <param name="parseMode"></param>
    /// <param name="entities"></param>
    /// <param name="linkPreviewOptions"></param>
    /// <param name="disableNotification"></param>
    /// <param name="replyParameters"></param>
    /// <param name="replyMarkup"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <see cref="Telegram.Bot.TelegramBotClientExtensions.SendMessage"/>
    protected async Task<Message> ReplyTextMessageAsync(string text,
        ParseMode parseMode = default,
        IEnumerable<MessageEntity>? entities = null,
        LinkPreviewOptions? linkPreviewOptions = null,
        bool disableNotification = false,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default) =>
        await BotData.Bot.SendMessage(TelegramChat!.Id, text, parseMode, replyParameters, replyMarkup, linkPreviewOptions,
            entities: entities, disableNotification: disableNotification, cancellationToken: cancellationToken);

    /// <summary>
    /// Shortcut to send a sticker to current chat
    /// </summary>
    /// <param name="sticker"></param>
    /// <param name="disableNotification"></param>
    /// <param name="replyParameters"></param>
    /// <param name="replyMarkup"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <see cref="Telegram.Bot.TelegramBotClientExtensions.SendSticker"/>
    protected async Task<Message> ReplyStickerAsync(InputFile sticker,
        bool disableNotification = false,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default) =>
        await BotData.Bot.SendSticker(TelegramChat!.Id, sticker, replyParameters, replyMarkup,
            disableNotification: disableNotification, cancellationToken: cancellationToken);

    /// <summary>
    /// Shortcut to send a photo to current chat
    /// </summary>
    /// <param name="photo"></param>
    /// <param name="caption"></param>
    /// <param name="parseMode"></param>
    /// <param name="captionEntities"></param>
    /// <param name="disableNotification"></param>
    /// <param name="replyParameters"></param>
    /// <param name="replyMarkup"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <see cref="Telegram.Bot.TelegramBotClientExtensions.SendPhoto"/>
    protected async Task<Message> ReplyPhotoAsync(InputFile photo,
        string? caption = null,
        ParseMode parseMode = default,
        IEnumerable<MessageEntity>? captionEntities = null,
        bool disableNotification = false,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default) =>
        await BotData.Bot.SendPhoto(TelegramChat!.Id, photo, caption, parseMode, replyParameters, replyMarkup,
            captionEntities: captionEntities, disableNotification: disableNotification, cancellationToken: cancellationToken);
}