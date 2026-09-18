using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Telegram.Bot.Advanced.IntegrationTests.Infrastructure;

/// <summary>
/// A temporary, per-test SQLite database file under <c>%TEMP%\tba-integration\&lt;guid&gt;\bot.db</c>. Real
/// relational semantics (composite primary keys, foreign keys, cascade deletes) are what motivate SQLite over the
/// unit suite's InMemory provider. Connection pooling is disabled so the file can be deleted without having to
/// clear the ADO.NET connection pool first.
/// </summary>
public sealed class TempSqliteDatabase : IAsyncDisposable {
    public string DirectoryPath { get; }
    public string FilePath { get; }
    public string ConnectionString { get; }

    private TempSqliteDatabase(string directoryPath, string filePath, string connectionString) {
        DirectoryPath = directoryPath;
        FilePath = filePath;
        ConnectionString = connectionString;
    }

    public static async Task<TempSqliteDatabase> CreateAsync(CancellationToken cancellationToken) {
        var directoryPath = Path.Combine(Path.GetTempPath(), "tba-integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        var filePath = Path.Combine(directoryPath, "bot.db");
        var connectionString = $"Data Source={filePath};Pooling=False";

        var database = new TempSqliteDatabase(directoryPath, filePath, connectionString);

        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        return database;
    }

    /// <summary>
    /// Creates a fresh context per call; tests must re-read through a new context to prove real persistence rather
    /// than a first-level cache hit.
    /// </summary>
    public IntegrationTelegramContext CreateContext() =>
        new(new DbContextOptionsBuilder<IntegrationTelegramContext>().UseSqlite(ConnectionString).Options);

    public void AddTo(IServiceCollection services) =>
        services.AddDbContext<IntegrationTelegramContext>(o => o.UseSqlite(ConnectionString));

    public async ValueTask DisposeAsync() {
        await using (var context = CreateContext()) {
            await context.Database.EnsureDeletedAsync();
        }

        try {
            Directory.Delete(DirectoryPath, recursive: true);
        }
        catch (IOException) {
            // Best effort: the OS may still hold a handle briefly after SQLite closes the file.
        }
    }
}
