using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.TestServer {
    public class DataSeeder {
        private readonly TelegramContext _context;

        public DataSeeder(TestTelegramContext context) {
            _context = context;
        }

        public void SeedData() {
            _context.Newsletters.Add(new Newsletter("startup", "Send a newsletter when the bot starts"));
        }
    }
}