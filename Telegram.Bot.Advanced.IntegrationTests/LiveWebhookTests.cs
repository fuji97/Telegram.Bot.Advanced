using System.Net;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.IntegrationTests.Infrastructure;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.IntegrationTests;

/// <summary>
/// Lane C: exercises the real webhook pipeline through a user-supplied public URL (e.g. an ngrok tunnel). Needs
/// <c>TBA_IT_BOT_TOKEN</c>, <c>TBA_IT_WEBHOOK_BASE_URL</c>, and <c>TBA_IT_WEBHOOK_LOCAL_PORT</c>; self-skips with
/// an actionable message otherwise.
/// </summary>
public sealed class LiveWebhookTests {
    private readonly ITestOutputHelper _output;

    public LiveWebhookTests(ITestOutputHelper output) {
        _output = output;
    }

    private static Task<TestWebhookHost> StartHostAsync(TempSqliteDatabase db, string token, Uri baseUrl, int port, CancellationToken cancellationToken) =>
        TestWebhookHost.StartAsync(db, new TestWebhookHostOptions {
            RealBotToken = token,
            PublicBaseUri = baseUrl,
            Port = port,
            SecretToken = IntegrationSettings.WebhookSecret,
            DeleteWebhookOnShutdown = true
        }, cancellationToken);

    [Fact]
    public async Task SetWebhook_ThroughHostedService_RegistersPublicUrlWithTelegram() {
        var ct = TestContext.Current.CancellationToken;
        var token = IntegrationSettings.RequireToken();
        var (baseUrl, port) = IntegrationSettings.RequireWebhook();

        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var host = await StartHostAsync(db, token, baseUrl, port, ct);

        var info = await host.Bot.GetWebhookInfo(ct);

        Assert.Equal($"{baseUrl.ToString().TrimEnd('/')}/telegram/itbot", info.Url);
        Assert.True(string.IsNullOrEmpty(info.LastErrorMessage), $"Telegram reported a webhook error: {info.LastErrorMessage}");
    }

    [Fact]
    public async Task PublicWebhookUrl_SyntheticUpdateWithSecret_DispatchesThroughTunnelAndPersists() {
        var ct = TestContext.Current.CancellationToken;
        var token = IntegrationSettings.RequireToken();
        var (baseUrl, port) = IntegrationSettings.RequireWebhook();
        var chatId = IntegrationSettings.RequireChatId();

        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var host = await StartHostAsync(db, token, baseUrl, port, ct);

        Message? sent = null;
        try {
            var update = UpdateFactory.TextMessage(chatId, "/echo hello", senderId: chatId);
            var response = await host.PostUpdateAsync(
                update, IntegrationSettings.WebhookSecret, new Uri(baseUrl, "/telegram/itbot"), ct);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await using var context = db.CreateContext();
            Assert.NotNull(await TelegramChat.GetAsync(context, chatId, ct));

            Assert.True(host.Recorder.SentMessages.TryDequeue(out sent));
            Assert.Equal("echo:hello", sent!.Text);
        }
        finally {
            if (sent is not null) {
                try {
                    await host.Bot.DeleteMessage(chatId, sent.Id, ct);
                }
                catch (ApiRequestException) {
                    // Best-effort cleanup only; a message the bot can no longer see is not a test failure.
                }
            }
        }
    }

    [Fact]
    public async Task PublicWebhookUrl_MissingSecretHeader_Returns401ThroughTunnel() {
        var ct = TestContext.Current.CancellationToken;
        var token = IntegrationSettings.RequireToken();
        var (baseUrl, port) = IntegrationSettings.RequireWebhook();

        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var host = await StartHostAsync(db, token, baseUrl, port, ct);

        var update = UpdateFactory.TextMessage(999_999, "/echo hello");
        var response = await host.PostUpdateAsync(update, secret: null, new Uri(baseUrl, "/telegram/itbot"), cancellationToken: ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await using var context = db.CreateContext();
        Assert.Empty(context.Users);
    }

    [Fact]
    public async Task Host_Shutdown_DeletesWebhookFromTelegram() {
        var ct = TestContext.Current.CancellationToken;
        var token = IntegrationSettings.RequireToken();
        var (baseUrl, port) = IntegrationSettings.RequireWebhook();

        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        // Not `await using`: this test explicitly disposes once and asserts the effect, so it must not dispose twice.
        var host = await StartHostAsync(db, token, baseUrl, port, ct);
        var bot = host.Bot;

        await host.DisposeAsync();

        var info = await bot.GetWebhookInfo(ct);
        Assert.True(string.IsNullOrEmpty(info.Url));
    }

    [Fact]
    public async Task RealTelegramDelivery_ManualMessage_IsDispatchedWithinWaitWindow() {
        var ct = TestContext.Current.CancellationToken;
        var token = IntegrationSettings.RequireToken();
        var (baseUrl, port) = IntegrationSettings.RequireWebhook();
        Assert.SkipUnless(IntegrationSettings.InboundWaitSeconds > 0,
            "Set TBA_IT_INBOUND_WAIT_SECONDS>0 and send /echo manual to the bot while the test runs to verify real Telegram delivery.");

        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var host = await StartHostAsync(db, token, baseUrl, port, ct);

        var webhookUrl = new Uri(baseUrl, "/telegram/itbot");
        _output.WriteLine(
            $"Send \"/echo manual\" to the bot now. Listening at {webhookUrl} for {IntegrationSettings.InboundWaitSeconds}s.");

        var deadline = DateTime.UtcNow.AddSeconds(IntegrationSettings.InboundWaitSeconds);
        while (host.Recorder.HandlerCalls.IsEmpty && DateTime.UtcNow < deadline) {
            await Task.Delay(500, ct);
        }

        Assert.False(host.Recorder.HandlerCalls.IsEmpty, $"No update arrived at {webhookUrl} within the wait window.");
    }
}
