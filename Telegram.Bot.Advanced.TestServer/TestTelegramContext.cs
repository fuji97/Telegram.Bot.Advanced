using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.DbContexts;

namespace TestServer {
    public class TestTelegramContext : TelegramContext {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseInMemoryDatabase("database_test");
        }
    }
}