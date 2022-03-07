using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Telegram.Bot.Advanced.DbContexts {
    public class NewsletterChat {
        public string NewsletterKey { get; set; } = null!;

        [ForeignKey(nameof(NewsletterKey))]
        public Newsletter Newsletter { get; set; } = null!;

        public long ChatId { get; set; }
        [ForeignKey(nameof(ChatId)), JsonIgnore]
        public TelegramChat Chat { get; set; } = null!;

        public NewsletterChat(Newsletter newsletter, TelegramChat chat) {
            Newsletter = newsletter;
            Chat = chat;
        }

        public NewsletterChat() {
        }
    }
}