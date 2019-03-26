using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Advanced.Controller {
    public class TelegramController<TContext> : ITelegramController<TContext> where TContext : TelegramContext {
        public MessageCommand MessageCommand { get; set; }
        public TContext TelegramContext { get; set; }
        public TelegramChat TelegramChat { get; set; }
        public ITelegramBotData BotData { get; set; }
        public Update Update { get; set; }

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

        protected async Task ReplyStickerAsync(InputOnlineFile sticker,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null,
            CancellationToken cancellationToken = default (CancellationToken)) {
            await BotData.Bot.SendStickerAsync(TelegramChat.Id, sticker, disableNotification, replyToMessageId,
                replyMarkup, cancellationToken);
        }

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