using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Controller {
    public class TelegramController : ITelegramController {
        public MessageCommand MessageCommand { get; set; }
        public TelegramContext TelegramContext { get; set; }
        public TelegramChat TelegramChat { get; set; }
    }
}