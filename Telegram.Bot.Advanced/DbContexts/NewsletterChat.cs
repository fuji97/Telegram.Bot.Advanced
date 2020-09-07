using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Telegram.Bot.Advanced.DbContexts {
    public class NewsletterChat {
        public string NewsletterKey { get; set; }
        [ForeignKey(nameof(NewsletterKey))]
        public Newsletter Newsletter { get; set; }
        public long ChatId { get; set; }
        [ForeignKey(nameof(ChatId)), JsonIgnore]
        public TelegramChat Chat { get; set; }
        
        public NewsletterChat(Newsletter newsletter, TelegramChat chat) {
            Newsletter = newsletter;
            Chat = chat;
        }

        public NewsletterChat() {
        }
    }
}