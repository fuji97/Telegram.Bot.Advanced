using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Controller {
    public interface ITelegramController<TContext> where TContext : TelegramContext {
        MessageCommand MessageCommand { set; }
        TContext TelegramContext { set; }
        TelegramChat TelegramChat { set; }
        ITelegramBotData BotData { set; }
    }
}