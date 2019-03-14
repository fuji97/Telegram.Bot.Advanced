using System.Collections.Generic;

namespace Telegram.Bot.Advanced.Holder {
    public interface ITelegramHolder : IEnumerable<ITelegramBotData> {

        ITelegramBotData Get(string key);
    }
}