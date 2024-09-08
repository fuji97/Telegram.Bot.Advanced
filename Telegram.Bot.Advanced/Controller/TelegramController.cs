using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Shortcut to send a message to current chat
    /// </summary>
    /// <param name="text"></param>
    /// <param name="parseMode"></param>
    /// <param name="entities"></param>
    /// <param name="linkPreviewOptions"></param>
    /// <param name="disableNotification"></param>
    /// <param name="protectContent"></param>
    /// <param name="messageEffectId"></param>
    /// <param name="replyParameters"></param>
    /// <param name="replyMarkup"></param>
    /// <param name="businessConnectionId"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="messageThreadId"></param>
    /// <returns></returns>
    /// <see cref="Telegram.Bot.TelegramBotClient.SendTextMessageAsync"/>
    protected async Task<Message> ReplyTextMessageAsync(string text, 
        int? messageThreadId = null, 
        ParseMode parseMode = ParseMode.None, 
        IEnumerable<MessageEntity>? entities = null, 
        LinkPreviewOptions? linkPreviewOptions = null, 
        bool disableNotification = false, 
        bool protectContent = false, 
        string? messageEffectId = null, 
        ReplyParameters? replyParameters = null, 
        IReplyMarkup? replyMarkup = null, 
        string? businessConnectionId = null,
        CancellationToken cancellationToken = default (CancellationToken)) {
        return await BotData.Bot.SendTextMessageAsync(TelegramChat!.Id, text, messageThreadId, parseMode, entities,
            linkPreviewOptions, disableNotification, protectContent, messageEffectId, replyParameters, replyMarkup,
            businessConnectionId, cancellationToken);
    }

    /// <summary>
    /// Shortcut to send a sticker to current chat
    /// </summary>
    /// <param name="sticker"></param>
    /// <param name="emoji"></param>
    /// <param name="disableNotification"></param>
    /// <param name="replyParameters"></param>
    /// <param name="replyMarkup"></param>
    /// <param name="businessConnectionId"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="messageThreadId"></param>
    /// <param name="protectContent"></param>
    /// <param name="messageEffectId"></param>
    /// <returns></returns>
    /// <see cref="Telegram.Bot.TelegramBotClient.SendStickerAsync"/>
    protected async Task<Message> ReplyStickerAsync(InputFile sticker, 
        int? messageThreadId = null, 
        string? emoji = null, 
        bool disableNotification = false, 
        bool protectContent = false, 
        string? messageEffectId = null, 
        ReplyParameters? replyParameters = null, 
        IReplyMarkup? replyMarkup = null, 
        string? businessConnectionId = null,
        CancellationToken cancellationToken = default (CancellationToken)) {
        return await BotData.Bot.SendStickerAsync(TelegramChat!.Id, sticker, messageThreadId, emoji,
            disableNotification, protectContent, messageEffectId, replyParameters, replyMarkup,
            businessConnectionId, cancellationToken);
    }

    /// <summary>
    /// Shortcut to send a photo to current chat
    /// </summary>
    /// <param name="photo"></param>
    /// <param name="messageThreadId"></param>
    /// <param name="caption"></param>
    /// <param name="parseMode"></param>
    /// <param name="captionEntities"></param>
    /// <param name="hasSpoiler"></param>
    /// <param name="disableNotification"></param>
    /// <param name="replyParameters"></param>
    /// <param name="replyMarkup"></param>
    /// <param name="businessConnectionId"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="showCaptionAboveMedia"></param>
    /// <param name="protectContent"></param>
    /// <param name="messageEffectId"></param>
    /// <returns></returns>
    /// <see cref="Telegram.Bot.TelegramBotClient.SendPhotoAsync"/>
    protected async Task<Message> ReplyPhotoAsync(InputFile photo, 
        int? messageThreadId = null, 
        string? caption = null, 
        ParseMode parseMode = ParseMode.None, 
        IEnumerable<MessageEntity>? captionEntities = null, 
        bool showCaptionAboveMedia = false, 
        bool hasSpoiler = false, 
        bool disableNotification = false, 
        bool protectContent = false, 
        string? messageEffectId = null, 
        ReplyParameters? replyParameters = null, 
        IReplyMarkup? replyMarkup = null, 
        string? businessConnectionId = null,
        CancellationToken cancellationToken = default (CancellationToken)) {
        return await BotData.Bot.SendPhotoAsync(TelegramChat!.Id, photo, messageThreadId, caption, parseMode,
            captionEntities, showCaptionAboveMedia, hasSpoiler, disableNotification, protectContent,
            messageEffectId, replyParameters, replyMarkup, businessConnectionId, cancellationToken);
    }
}