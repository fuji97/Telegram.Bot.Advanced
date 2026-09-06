using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.TestServer.SeedData;

public sealed class DataSeeder {
    private readonly TestTelegramContext _context;

    public DataSeeder(TestTelegramContext context) {
        _context = context;
    }

    /// <summary>
    /// Idempotent: safe to call on every startup without producing duplicate seed rows.
    /// </summary>
    public async Task SeedDataAsync(CancellationToken cancellationToken = default) {
        const string newsletterKey = "startup";

        var exists = await _context.Newsletters.AnyAsync(n => n.Key == newsletterKey, cancellationToken);
        if (!exists) {
            _context.Newsletters.Add(new Newsletter(newsletterKey, "Send a newsletter when the bot starts"));
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
