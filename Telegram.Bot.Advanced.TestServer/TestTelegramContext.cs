using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.TestServer;

public sealed class TestTelegramContext : TelegramContext {
    public TestTelegramContext(DbContextOptions<TestTelegramContext> options) : base(options) {
    }
}
