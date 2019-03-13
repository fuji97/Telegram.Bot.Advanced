using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Holder {
    public class TelegramHolder : ITelegramHolder {
        private Dictionary<string, TelegramBotData> Bots { get; }

        public TelegramHolder(IEnumerable<TelegramBotData> bots) {
            Bots = new Dictionary<string, TelegramBotData>();
            foreach (var bot in bots) {
                Bots[bot.Endpoint] = bot;
            }
        }

        public TelegramBotData Get(string key) {
            return Bots[key];
        }

        public IEnumerator<TelegramBotData> GetEnumerator() {
            return Bots.Select(pair => pair.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}