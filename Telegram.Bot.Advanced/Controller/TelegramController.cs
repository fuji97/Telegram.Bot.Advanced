using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Advanced.Controller {
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
        /// <param name="mode"></param>
        /// <param name="entities"></param>
        /// <param name="disableWebPagePreview"></param>
        /// <param name="disableNotification"></param>
        /// <param name="replyToMessageId"></param>
        /// <param name="allowSendingWithoutReply"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <see cref="Telegram.Bot.TelegramBotClient.SendTextMessageAsync"/>
        protected async Task<Message> ReplyTextMessageAsync(string text, 
            ParseMode? mode = null,
            IEnumerable<MessageEntity>? entities = null,
            bool? disableWebPagePreview = null,
            bool? disableNotification = null,
            int? replyToMessageId = null,
            bool? allowSendingWithoutReply = null,
            IReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default (CancellationToken)) {
            return await BotData.Bot.SendTextMessageAsync(TelegramChat!.Id, text, mode, entities, disableWebPagePreview, disableNotification, 
                replyToMessageId, allowSendingWithoutReply, replyMarkup, cancellationToken);
        }

        /// <summary>
        /// Shortcut to send a sticker to current chat
        /// </summary>
        /// <param name="sticker"></param>
        /// <param name="disableNotification"></param>
        /// <param name="replyToMessageId"></param>
        /// <param name="allowSendingWithoutReply"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <see cref="Telegram.Bot.TelegramBotClient.SendStickerAsync"/>
        protected async Task<Message> ReplyStickerAsync(InputOnlineFile sticker,
            bool? disableNotification = null,
            int? replyToMessageId = null,
            bool? allowSendingWithoutReply = null,
            IReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default (CancellationToken)) {
            return await BotData.Bot.SendStickerAsync(TelegramChat!.Id, sticker, disableNotification, replyToMessageId,
                allowSendingWithoutReply, replyMarkup, cancellationToken);
        }

        /// <summary>
        /// Shortcut to send a photo to current chat
        /// </summary>
        /// <param name="photo"></param>
        /// <param name="caption"></param>
        /// <param name="parseMode"></param>
        /// <param name="captionEntities"></param>
        /// <param name="disableNotification"></param>
        /// <param name="replyToMessageId"></param>
        /// <param name="allowSendingWithoutReply"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <see cref="Telegram.Bot.TelegramBotClient.SendPhotoAsync"/>
        protected async Task<Message> ReplyPhotoAsync(InputOnlineFile photo,
            string? caption = null,
            ParseMode? parseMode = null,
            IEnumerable<MessageEntity>? captionEntities = null,
            bool? disableNotification = null,
            int? replyToMessageId = null,
            bool? allowSendingWithoutReply = null,
            IReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default (CancellationToken)) {
            return await BotData.Bot.SendPhotoAsync(TelegramChat!.Id, photo, caption, parseMode, captionEntities, disableNotification,
                replyToMessageId, allowSendingWithoutReply, replyMarkup, cancellationToken);
        }
    }
}