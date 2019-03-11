using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Controller {
    public interface ITelegramController {
        MessageCommand MessageCommand { set; }
        TelegramContext TelegramContext { set; }
        TelegramChat TelegramChat { set; }
    }
}