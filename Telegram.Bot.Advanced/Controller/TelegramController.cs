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
        public MessageCommand MessageCommand { get; set; }
        public TContext TelegramContext { get; set; }
        public TelegramChat TelegramChat { get; set; }
        public ITelegramBotData BotData { get; set; }
        public Update Update { get; set; }

        /// <summary>
        /// Shortcut to send a message to current chat
        /// </summary>
        /// <param name="text"></param>
        /// <param name="mode"></param>
        /// <param name="disableWebPagePreview"></param>
        /// <param name="disableNotification"></param>
        /// <param name="replyToMessageId"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <see cref="Telegram.Bot.TelegramBotClient.SendTextMessageAsync"/>
        protected async Task ReplyTextMessageAsync(string text, 
            ParseMode mode = ParseMode.Default, 
            bool disableWebPagePreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null,
            CancellationToken cancellationToken = default (CancellationToken)) {
            await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, text, mode, disableWebPagePreview, disableNotification, 
                replyToMessageId, replyMarkup, cancellationToken);
        }

        /// <summary>
        /// Shortcut to send a sticker to current chat
        /// </summary>
        /// <param name="sticker"></param>
        /// <param name="disableNotification"></param>
        /// <param name="replyToMessageId"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <see cref="Telegram.Bot.TelegramBotClient.SendStickerAsync"/>
        protected async Task ReplyStickerAsync(InputOnlineFile sticker,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null,
            CancellationToken cancellationToken = default (CancellationToken)) {
            await BotData.Bot.SendStickerAsync(TelegramChat.Id, sticker, disableNotification, replyToMessageId,
                replyMarkup, cancellationToken);
        }

        /// <summary>
        /// Shortcut to send a photo to current chat
        /// </summary>
        /// <param name="photo"></param>
        /// <param name="caption"></param>
        /// <param name="parseMode"></param>
        /// <param name="disableNotification"></param>
        /// <param name="replyToMessageId"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <see cref="Telegram.Bot.TelegramBotClient.SendPhotoAsync"/>
        protected async Task ReplyPhotoAsync(InputOnlineFile photo,
            string caption = null,
            ParseMode parseMode = ParseMode.Default,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null,
            CancellationToken cancellationToken = default (CancellationToken)) {
            await BotData.Bot.SendPhotoAsync(TelegramChat.Id, photo, caption, parseMode, disableNotification,
                replyToMessageId, replyMarkup, cancellationToken);
        }
    }
}