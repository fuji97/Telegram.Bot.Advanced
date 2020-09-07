using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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

        public static bool IsGroup(this Chat chat) {
            return chat.Type == ChatType.Group || chat.Type == ChatType.Supergroup;
        }
    }
}