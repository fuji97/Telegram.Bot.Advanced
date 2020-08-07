using System;
using System.Threading.Tasks;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Models {
    public class StartupNewsletter {
        public string NewsletterKey { get; }
        public Action<ITelegramBotData, TelegramChat, IServiceProvider> Action { get; }

        public StartupNewsletter(string newsletterKey, Action<ITelegramBotData, TelegramChat, IServiceProvider> action) {
            NewsletterKey = newsletterKey;
            Action = action;
        }
    }
}