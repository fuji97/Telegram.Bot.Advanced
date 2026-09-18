using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.IntegrationTests.Infrastructure;

/// <summary>
/// Minimal, integration-project-local <see cref="TelegramContext"/> backed by a real SQLite database
/// (<see cref="TempSqliteDatabase"/>), proving composite keys, foreign keys, and cascade deletes that the
/// unit suite's InMemory provider does not enforce.
/// </summary>
public sealed class IntegrationTelegramContext : TelegramContext {
    public IntegrationTelegramContext(DbContextOptions<IntegrationTelegramContext> options) : base(options) {
    }
}
