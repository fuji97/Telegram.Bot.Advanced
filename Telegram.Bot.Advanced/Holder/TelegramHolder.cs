using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Telegram.Bot.Advanced.Holder {
    public class TelegramHolder : ITelegramHolder {
        private Dictionary<string, ITelegramBotData> Bots { get; }

        public TelegramHolder(IEnumerable<ITelegramBotData> bots) {
            Bots = new Dictionary<string, ITelegramBotData>();
            foreach (var bot in bots) {
                Bots[bot.Endpoint] = bot;
            }
        }

        public ITelegramBotData Get(string key) {
            return Bots[key];
        }

        public IEnumerator<ITelegramBotData> GetEnumerator() {
            return Bots.Select(pair => pair.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}