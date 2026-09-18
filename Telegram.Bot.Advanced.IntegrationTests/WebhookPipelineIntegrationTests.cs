using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.IntegrationTests.Infrastructure;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.IntegrationTests;

/// <summary>
/// Lane A: a real Kestrel host, the real webhook pipeline, and a temp SQLite database, backed by
/// <see cref="FakeTelegramApi"/> so no credentials are required. Runs on every <c>dotnet test</c> invocation.
/// </summary>
public sealed class WebhookPipelineIntegrationTests {
    private const string Secret = "lane-a-secret";

    /// <summary>
    /// Reserves a free loopback port up front: the webhook URL registered with the fake Telegram API must be known
    /// before the host binds, so an ephemeral (zero) port cannot be used together with the webhook transport.
    /// </summary>
    private static int FreePort() {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try {
            return ((IPEndPoint) listener.LocalEndpoint).Port;
        }
        finally {
            listener.Stop();
        }
    }

    private static Task<TestWebhookHost> StartHostAsync(TempSqliteDatabase db, CancellationToken cancellationToken, bool deleteWebhookOnShutdown = true) =>
        TestWebhookHost.StartAsync(db, new TestWebhookHostOptions { SecretToken = Secret, Port = FreePort(), DeleteWebhookOnShutdown = deleteWebhookOnShutdown }, cancellationToken);

    [Fact]
    public async Task Webhook_ValidSecretAndCommand_Returns200PersistsChatAndSendsReply() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var host = await StartHostAsync(db, ct);

        var update = UpdateFactory.TextMessage(101, "/echo hello", chatUsername: "lane_a_user");
        var response = await host.PostUpdateAsync(update, Secret, cancellationToken: ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"Ok\"", await response.Content.ReadAsStringAsync(ct));

        await using var context = db.CreateContext();
        var chat = await TelegramChat.GetAsync(context, 101, ct);
        Assert.NotNull(chat);
        Assert.Equal("lane_a_user", chat!.Username);
        Assert.Equal(ChatType.Private, chat.Type);

        var lastSendMessage = Assert.Single(host.Api!.Requests, r => r.Method == "sendMessage");
        using var doc = JsonDocument.Parse(lastSendMessage.Body!);
        Assert.Equal("echo:hello", doc.RootElement.GetProperty("text").GetString());
        Assert.Equal(101, doc.RootElement.GetProperty("chat_id").GetInt64());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-secret")]
    [InlineData("")]
    public async Task Webhook_WrongSecretHeader_Returns401AndWritesNothing(string? secret) {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var host = await StartHostAsync(db, ct);

        var update = UpdateFactory.TextMessage(102, "/echo hello");
        var response = await host.PostUpdateAsync(update, secret, cancellationToken: ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await using var context = db.CreateContext();
        Assert.Empty(context.Users);
    }

    [Fact]
    public async Task Webhook_PostToUnmappedEndpointPath_Returns404() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var host = await StartHostAsync(db, ct);

        var update = UpdateFactory.TextMessage(103, "/echo hello");
        var response = await host.PostUpdateAsync(
            update, Secret, target: new Uri(host.BaseAddress, "/telegram/other"), cancellationToken: ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_HandlerThrows_Returns500ButChatAlreadyPersisted() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var host = await StartHostAsync(db, ct);

        var update = UpdateFactory.TextMessage(104, "/boom");
        var response = await host.PostUpdateAsync(update, Secret, cancellationToken: ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        await using var context = db.CreateContext();
        Assert.NotNull(await TelegramChat.GetAsync(context, 104, ct));
    }

    [Fact]
    public async Task Webhook_TwoRequests_DriveChatStateMachineThroughRealDatabase() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var host = await StartHostAsync(db, ct);

        var enter = UpdateFactory.TextMessage(105, "/state keep", updateId: 1);
        Assert.Equal(HttpStatusCode.OK, (await host.PostUpdateAsync(enter, Secret, cancellationToken: ct)).StatusCode);

        var leave = UpdateFactory.TextMessage(105, "/state", updateId: 2);
        Assert.Equal(HttpStatusCode.OK, (await host.PostUpdateAsync(leave, Secret, cancellationToken: ct)).StatusCode);

        Assert.Contains("state:enter", host.Recorder.HandlerCalls);
        Assert.Contains("state:leave:keep", host.Recorder.HandlerCalls);

        await using var context = db.CreateContext();
        var chat = await TelegramChat.GetAsync(context, 105, ct);
        Assert.NotNull(chat);
        Assert.Null(chat!.State);
        Assert.Equal("keep", chat["last"]);
    }

    [Fact]
    public async Task Webhook_SameUpdateIdTwice_IsDispatchedTwice() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var host = await StartHostAsync(db, ct);

        var update = UpdateFactory.TextMessage(106, "/echo hi", updateId: 999);
        await host.PostUpdateAsync(update, Secret, cancellationToken: ct);
        await host.PostUpdateAsync(update, Secret, cancellationToken: ct);

        Assert.Equal(2, host.Recorder.HandlerCalls.Count(call => call == "echo"));
        Assert.Equal(2, host.Api!.CallCount("sendMessage"));
    }

    [Fact]
    public async Task WebhookHostedService_RegistersUrlComposedFromBaseUriBasePathAndEndpoint() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        var port = FreePort();
        await using var host = await TestWebhookHost.StartAsync(
            db, new TestWebhookHostOptions { SecretToken = Secret, Port = port }, ct);

        var setWebhookRequest = Assert.Single(host.Api!.Requests, r => r.Method == "setWebhook");
        using var doc = JsonDocument.Parse(setWebhookRequest.Body!);
        Assert.Equal($"http://127.0.0.1:{port}/telegram/itbot", doc.RootElement.GetProperty("url").GetString());
        Assert.Equal(Secret, doc.RootElement.GetProperty("secret_token").GetString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Host_Shutdown_DeletesWebhookOnlyWhenConfigured(bool deleteOnShutdown) {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        var host = await StartHostAsync(db, ct, deleteOnShutdown);
        var api = host.Api!;

        await host.DisposeAsync();

        Assert.Equal(deleteOnShutdown ? 1 : 0, api.CallCount("deleteWebhook"));
    }
}
