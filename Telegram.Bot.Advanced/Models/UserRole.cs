using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Models {
    public class UserRole {
        public long UserId { get; set; }
        public string Username { get; set; }
        public SelectUserBy SelectUserBy { get; set; }
        public ChatRole Role { get; set; }

        public UserRole(long userId, ChatRole role) {
            UserId = userId;
            SelectUserBy = SelectUserBy.UserId;
            Role = role;
        }
        
        public UserRole(string username, ChatRole role) {
            Username = username;
            SelectUserBy = SelectUserBy.Username;
            Role = role;
        }

        public bool Equals(TelegramChat chat) {
            switch (SelectUserBy) {
                case SelectUserBy.Username:
                    return Username == chat.Username;
                case SelectUserBy.UserId:
                    return UserId == chat.Id;
                default:
                    return false;
            }
        }
    }
}