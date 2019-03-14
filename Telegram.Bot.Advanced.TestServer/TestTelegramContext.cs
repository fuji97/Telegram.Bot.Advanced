using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.TestServer {
    public class TestTelegramContext : TelegramContext {
        
        public TestTelegramContext() {
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseInMemoryDatabase("database_test");
        }
    }
}