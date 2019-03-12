using System.Collections.Generic;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Holder {
    public class TelegramHolder<TContext, TController> : ITelegramHolder<TContext, TController> 
        where TContext : TelegramContext, new()
        where TController : class, ITelegramController<TContext>, new() {
        private Dictionary<string, TelegramBotData<TContext, TController>> Bots { get; }

        public TelegramHolder(IEnumerable<TelegramBotData<TContext, TController>> bots) {
            Bots = new Dictionary<string, TelegramBotData<TContext, TController>>();
            foreach (var bot in bots) {
                Bots[bot.Endpoint] = bot;
            }
        }

        public TelegramBotData<TContext, TController> Get(string key) {
            return Bots[key];
        }
    }
}