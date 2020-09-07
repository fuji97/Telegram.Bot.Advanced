using System.Collections.Generic;

namespace Telegram.Bot.Advanced.Core.Holder {
    public interface ITelegramHolder : IEnumerable<ITelegramBotData> {

        ITelegramBotData Get(string key);
    }
}