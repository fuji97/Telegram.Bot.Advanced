using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Controller {
    public class TelegramController<TContext> : ITelegramController<TContext> where TContext : TelegramContext {
        public MessageCommand MessageCommand { get; set; }
        public TContext TelegramContext { get; set; }
        public TelegramChat TelegramChat { get; set; }
        public ITelegramBotData BotData { get; set; }
        public Update Update { get; set; }
    }
}