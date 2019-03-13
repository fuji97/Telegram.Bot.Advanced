using System.Collections.Generic;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Holder {
    public interface ITelegramHolder : IEnumerable<ITelegramBotData> {

        ITelegramBotData Get(string key);
    }
}