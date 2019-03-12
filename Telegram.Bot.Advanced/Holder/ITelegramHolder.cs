using System.Collections.Generic;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Holder {
    public interface ITelegramHolder<TContext, TController> where TContext : TelegramContext, new()
        where TController : class, ITelegramController<TContext>, new() {

        TelegramBotData<TContext, TController> Get(string key);
    }
}