using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Tests.Infrastructure;

/// <summary>
/// Minimal, test-project-local <see cref="TelegramContext"/> used by every EF-backed test. Each test builds its own
/// instance (or shares one database name across several instances) via <see cref="Create"/>.
/// </summary>
public sealed class TestTelegramContext : TelegramContext {
    public TestTelegramContext(DbContextOptions<TestTelegramContext> options) : base(options) {
    }

    /// <summary>
    /// Creates a context backed by a brand-new (or explicitly shared) named InMemory database.
    /// </summary>
    public static TestTelegramContext Create(string? databaseName = null) =>
        new(new DbContextOptionsBuilder<TestTelegramContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options);
}
