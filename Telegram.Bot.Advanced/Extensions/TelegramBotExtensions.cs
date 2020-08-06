using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Extensions {
    public static class TelegramBotExtensions {
        public static TelegramChat ToModel(this User user) {
            return new TelegramChat() {
                Id = user.Id,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }
        
    }
}