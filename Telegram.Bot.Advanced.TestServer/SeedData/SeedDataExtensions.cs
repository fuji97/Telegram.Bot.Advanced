namespace Telegram.Bot.Advanced.TestServer.SeedData;

public static class SeedDataExtensions {
    public static async Task SeedDataAsync(this IApplicationBuilder app, CancellationToken cancellationToken = default) {
        await using var scope = app.ApplicationServices.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<TestTelegramContext>();
        await new DataSeeder(context).SeedDataAsync(cancellationToken);
    }
}
