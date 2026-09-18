using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Advanced.Tests.Infrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Tests;

/// <summary>
/// Covers the webhook entry point (<c>TelegramRouting.HandleAsync</c>, invoked via reflection since its containing
/// type is internal) and the transport hosted services, resolved exclusively through the public
/// <see cref="ServiceCollectionExtensions"/> DI extensions.
/// </summary>
public sealed class WebhookAndHostingTests {
    private const string SecretHeader = "X-Telegram-Bot-Api-Secret-Token";

    private static IServiceProvider BuildRoutingServices(TelegramWebhookOptions options, ITelegramHolder holder) {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(holder);
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(IServiceProvider services, string body, string? secretHeaderValue) {
        var context = new DefaultHttpContext { RequestServices = services };
        var bytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        if (secretHeaderValue is not null) {
            context.Request.Headers[SecretHeader] = secretHeaderValue;
        }

        return context;
    }

    private static string SerializeUpdate(Update update) => JsonSerializer.Serialize(update, JsonBotAPI.Options);

    private static int StatusCodeOf(IResult result) => Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode!.Value;

    // ---- HandleAsync ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_ValidSecretAndKnownEndpoint_DispatchesUpdateAndReturns200() {
        var dispatcher = Substitute.For<IDispatcher>();
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("mybot");
        botData.Dispatcher.Returns(dispatcher);
        var holder = new TelegramHolder([botData]);
        var services = BuildRoutingServices(new TelegramWebhookOptions { SecretToken = "s3cr3t" }, holder);
        var update = new Update { Id = 42, Message = new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }, Text = "hi" } };
        var context = CreateContext(services, SerializeUpdate(update), "s3cr3t");

        var result = await TelegramRoutingInvoker.HandleAsync(context, "mybot");

        Assert.Equal(200, StatusCodeOf(result));
        await dispatcher.Received(1).DispatchUpdateAsync(
            Arg.Is<Update>(u => u.Id == 42), services, context.RequestAborted);
    }

    public enum SecretScenario { Missing, WrongLength, RightLengthWrongValue, ServerSecretUnset }

    [Theory]
    [InlineData(SecretScenario.Missing)]
    [InlineData(SecretScenario.WrongLength)]
    [InlineData(SecretScenario.RightLengthWrongValue)]
    [InlineData(SecretScenario.ServerSecretUnset)]
    public async Task HandleAsync_InvalidSecret_Returns401AndDoesNotDispatch(SecretScenario scenario) {
        var dispatcher = Substitute.For<IDispatcher>();
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("mybot");
        botData.Dispatcher.Returns(dispatcher);
        var holder = new TelegramHolder([botData]);
        const string configuredSecret = "correct-secret";
        var options = new TelegramWebhookOptions { SecretToken = scenario == SecretScenario.ServerSecretUnset ? null : configuredSecret };
        var services = BuildRoutingServices(options, holder);

        string? header = scenario switch {
            SecretScenario.Missing => null,
            SecretScenario.WrongLength => "short",
            SecretScenario.RightLengthWrongValue => new string('x', configuredSecret.Length),
            SecretScenario.ServerSecretUnset => configuredSecret,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var update = new Update { Id = 1, Message = new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }, Text = "hi" } };
        var context = CreateContext(services, SerializeUpdate(update), header);

        var result = await TelegramRoutingInvoker.HandleAsync(context, "mybot");

        Assert.Equal(401, StatusCodeOf(result));
        await dispatcher.DidNotReceive().DispatchUpdateAsync(Arg.Any<Update>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("{not-valid-json")]
    [InlineData("null")]
    public async Task HandleAsync_MalformedOrNullJsonBody_Returns400(string body) {
        var dispatcher = Substitute.For<IDispatcher>();
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("mybot");
        botData.Dispatcher.Returns(dispatcher);
        var holder = new TelegramHolder([botData]);
        var services = BuildRoutingServices(new TelegramWebhookOptions { SecretToken = "s3cr3t" }, holder);
        var context = CreateContext(services, body, "s3cr3t");

        var result = await TelegramRoutingInvoker.HandleAsync(context, "mybot");

        Assert.Equal(400, StatusCodeOf(result));
        await dispatcher.DidNotReceive().DispatchUpdateAsync(Arg.Any<Update>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownEndpoint_Returns404AndDoesNotDispatch() {
        var dispatcher = Substitute.For<IDispatcher>();
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("known-bot");
        botData.Dispatcher.Returns(dispatcher);
        var holder = new TelegramHolder([botData]);
        var services = BuildRoutingServices(new TelegramWebhookOptions { SecretToken = "s3cr3t" }, holder);
        var update = new Update { Id = 1, Message = new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }, Text = "hi" } };
        var context = CreateContext(services, SerializeUpdate(update), "s3cr3t");

        var result = await TelegramRoutingInvoker.HandleAsync(context, "unknown-bot");

        Assert.Equal(404, StatusCodeOf(result));
        await dispatcher.DidNotReceive().DispatchUpdateAsync(Arg.Any<Update>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
    }

    private sealed class BoomException() : Exception("dispatch-failed");

    [Fact]
    public async Task HandleAsync_DispatchThrows_ExceptionPropagatesOutOfHandleAsync() {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.DispatchUpdateAsync(Arg.Any<Update>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new BoomException()));
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("mybot");
        botData.Dispatcher.Returns(dispatcher);
        var holder = new TelegramHolder([botData]);
        var services = BuildRoutingServices(new TelegramWebhookOptions { SecretToken = "s3cr3t" }, holder);
        var update = new Update { Id = 1, Message = new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }, Text = "hi" } };
        var context = CreateContext(services, SerializeUpdate(update), "s3cr3t");

        await Assert.ThrowsAsync<BoomException>(() => TelegramRoutingInvoker.HandleAsync(context, "mybot"));
    }

    // ---- registration / hosted services ---------------------------------------------------------------------

    [Fact]
    public void AddTelegramHolder_RegistrationAlone_MakesNoBotApiCalls() {
        var (client, http) = TestBotClientFactory.Create();
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("mybot");
        botData.Bot.Returns(client);
        botData.Dispatcher.Returns(Substitute.For<IDispatcher>());

        var services = new ServiceCollection();
        services.AddTelegramHolder(botData);
        var provider = services.BuildServiceProvider();

        var holder = provider.GetRequiredService<ITelegramHolder>();

        Assert.Contains(botData, holder);
        Assert.Empty(http.Requests);
    }

    [Fact]
    public async Task TelegramPollingHostedService_StartAsync_CallsGetMeAndSetsUsernameBeforeReturning() {
        var (client, http) = TestBotClientFactory.Create();
        http.Enqueue("deleteWebhook", true);
        http.Enqueue("getMe", new User { Id = 1, IsBot = true, FirstName = "Bot", Username = "polling_bot" });
        http.SetDefaultResponse("getUpdates", Array.Empty<Update>());

        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("polling-bot");
        botData.Bot.Returns(client);
        botData.Dispatcher.Returns(Substitute.For<IDispatcher>());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(CancellationToken.None);
        services.AddSingleton(lifetime);
        services.AddTelegramPolling();
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);
        try {
            Assert.Equal("polling_bot", botData.Username);
            Assert.Equal(1, http.CallCount("getMe"));
            Assert.Equal(1, http.CallCount("deleteWebhook"));
        }
        finally {
            await hosted.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TelegramWebhookHostedService_StartAsync_CallsGetMeAndSetsUsernameBeforeReturning() {
        var (client, http) = TestBotClientFactory.Create();
        http.Enqueue("getMe", new User { Id = 2, IsBot = true, FirstName = "Bot", Username = "webhook_bot" });
        http.Enqueue("setWebhook", true);

        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("webhook-bot");
        botData.Bot.Returns(client);
        botData.BasePath.Returns("/telegram/");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddTelegramWebhooks(o => {
            o.BaseUri = new Uri("https://example.test/");
            o.SecretToken = "s3cr3t";
        });
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal("webhook_bot", botData.Username);
        Assert.Equal(1, http.CallCount("getMe"));
        Assert.Equal(1, http.CallCount("setWebhook"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddTelegramWebhooksAfterAddTelegramPolling_ThrowsInvalidOperationException(bool pollingFirst) {
        var services = new ServiceCollection();

        if (pollingFirst) {
            services.AddTelegramPolling();
            Assert.Throws<InvalidOperationException>(() => services.AddTelegramWebhooks(o => {
                o.BaseUri = new Uri("https://example.test/");
                o.SecretToken = "s3cr3t";
            }));
        }
        else {
            services.AddTelegramWebhooks(o => {
                o.BaseUri = new Uri("https://example.test/");
                o.SecretToken = "s3cr3t";
            });
            Assert.Throws<InvalidOperationException>(() => services.AddTelegramPolling());
        }
    }

    [Fact]
    public async Task TelegramPollingHostedService_ApplicationStoppingCancelled_StopsReceiveLoop() {
        var (client, http) = TestBotClientFactory.Create();
        http.Enqueue("deleteWebhook", true);
        http.Enqueue("getMe", new User { Id = 1, IsBot = true, FirstName = "Bot", Username = "poll_bot" });
        http.SetDefaultResponse("getUpdates", Array.Empty<Update>());

        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("poll-bot");
        botData.Bot.Returns(client);
        botData.Dispatcher.Returns(Substitute.For<IDispatcher>());

        using var lifetimeCts = new CancellationTokenSource();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(lifetimeCts.Token);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddSingleton(lifetime);
        services.AddTelegramPolling();
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);
        try {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (http.CallCount("getUpdates") < 3 && DateTime.UtcNow < deadline) {
                await Task.Delay(10, TestContext.Current.CancellationToken);
            }
            Assert.True(http.CallCount("getUpdates") >= 3, "The polling receive loop never started making requests.");

            await lifetimeCts.CancelAsync();
            await Task.Delay(150, TestContext.Current.CancellationToken);
            var countAfterCancel = http.CallCount("getUpdates");
            await Task.Delay(200, TestContext.Current.CancellationToken);
            var countStable = http.CallCount("getUpdates");

            Assert.Equal(countAfterCancel, countStable);
        }
        finally {
            await hosted.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    // ---- webhook startup validation --------------------------------------------------------------------------

    [Fact]
    public async Task TelegramWebhookHostedService_MissingBaseUri_ThrowsInvalidOperationException() {
        var (client, _) = TestBotClientFactory.Create();
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("webhook-bot");
        botData.Bot.Returns(client);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddTelegramWebhooks(o => { o.SecretToken = "s3cr3t"; });
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hosted.StartAsync(TestContext.Current.CancellationToken));
        Assert.Contains("BaseUri", exception.Message);
    }

    [Fact]
    public async Task TelegramWebhookHostedService_MissingSecretToken_ThrowsInvalidOperationException() {
        var (client, _) = TestBotClientFactory.Create();
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("webhook-bot");
        botData.Bot.Returns(client);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddTelegramWebhooks(o => { o.BaseUri = new Uri("https://example.test/"); });
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hosted.StartAsync(TestContext.Current.CancellationToken));
        Assert.Contains("SecretToken", exception.Message);
    }

    [Theory]
    [InlineData("https://example.test", "/telegram/", "my-bot", "https://example.test/telegram/my-bot")]
    [InlineData("https://example.test/", "telegram", "my-bot", "https://example.test/telegram/my-bot")]
    [InlineData("https://example.test/app/", "/telegram/", "bot with space", "https://example.test/app/telegram/bot%20with%20space")]
    public async Task TelegramWebhookHostedService_StartAsync_BuildsWebhookUrlFromBaseUriAndBotEndpoint(
        string baseUri, string basePath, string endpoint, string expectedUrl) {
        var (client, http) = TestBotClientFactory.Create();
        http.Enqueue("getMe", new User { Id = 2, IsBot = true, FirstName = "Bot", Username = "webhook_bot" });
        http.Enqueue("setWebhook", true);

        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns(endpoint);
        botData.Bot.Returns(client);
        botData.BasePath.Returns(basePath);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddTelegramWebhooks(o => {
            o.BaseUri = new Uri(baseUri);
            o.SecretToken = "s3cr3t";
        });
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);

        var request = Assert.Single(http.Requests, r => r.Method == "setWebhook");
        using var doc = JsonDocument.Parse(request.Body!);
        Assert.Equal(expectedUrl, doc.RootElement.GetProperty("url").GetString());
    }

    [Fact]
    public async Task TelegramWebhookHostedService_SetWebhookThrows_WrapsInInvalidOperationException() {
        var (client, http) = TestBotClientFactory.Create();
        http.Enqueue("getMe", new User { Id = 2, IsBot = true, FirstName = "Bot", Username = "webhook_bot" });
        // No "setWebhook" response queued: the fake transport throws for the unexpected call.

        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("webhook-bot");
        botData.Bot.Returns(client);
        botData.BasePath.Returns("/telegram/");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddTelegramWebhooks(o => {
            o.BaseUri = new Uri("https://example.test/");
            o.SecretToken = "s3cr3t";
        });
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hosted.StartAsync(TestContext.Current.CancellationToken));
        Assert.Contains("Failed to register the webhook for bot endpoint 'webhook-bot'", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TelegramWebhookHostedService_StopAsync_DeleteWebhookOnShutdownTheory(bool deleteOnShutdown) {
        var (client, http) = TestBotClientFactory.Create();
        http.Enqueue("getMe", new User { Id = 2, IsBot = true, FirstName = "Bot", Username = "webhook_bot" });
        http.Enqueue("setWebhook", true);
        http.Enqueue("deleteWebhook", true);

        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("webhook-bot");
        botData.Bot.Returns(client);
        botData.BasePath.Returns("/telegram/");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddTelegramWebhooks(o => {
            o.BaseUri = new Uri("https://example.test/");
            o.SecretToken = "s3cr3t";
            o.DeleteWebhookOnShutdown = deleteOnShutdown;
        });
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);
        await hosted.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(deleteOnShutdown ? 1 : 0, http.CallCount("deleteWebhook"));
    }

    [Fact]
    public async Task TelegramWebhookHostedService_StopAsync_DeleteWebhookThrows_SwallowsException() {
        var (client, http) = TestBotClientFactory.Create();
        http.Enqueue("getMe", new User { Id = 2, IsBot = true, FirstName = "Bot", Username = "webhook_bot" });
        http.Enqueue("setWebhook", true);
        // No "deleteWebhook" response queued: the fake transport throws for the unexpected call.

        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("webhook-bot");
        botData.Bot.Returns(client);
        botData.BasePath.Returns("/telegram/");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddTelegramWebhooks(o => {
            o.BaseUri = new Uri("https://example.test/");
            o.SecretToken = "s3cr3t";
            o.DeleteWebhookOnShutdown = true;
        });
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);
        await hosted.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, http.CallCount("deleteWebhook"));
    }

    [Fact]
    public async Task TelegramWebhookHostedService_StartAsync_MultipleBots_RegistersWebhookForEach() {
        var (clientA, httpA) = TestBotClientFactory.Create();
        httpA.Enqueue("getMe", new User { Id = 2, IsBot = true, FirstName = "Bot", Username = "bot_a" });
        httpA.Enqueue("setWebhook", true);
        var (clientB, httpB) = TestBotClientFactory.Create();
        httpB.Enqueue("getMe", new User { Id = 3, IsBot = true, FirstName = "Bot", Username = "bot_b" });
        httpB.Enqueue("setWebhook", true);

        var botA = Substitute.For<ITelegramBotData>();
        botA.Endpoint.Returns("bot-a");
        botA.Bot.Returns(clientA);
        botA.BasePath.Returns("/telegram/");
        var botB = Substitute.For<ITelegramBotData>();
        botB.Endpoint.Returns("bot-b");
        botB.Bot.Returns(clientB);
        botB.BasePath.Returns("/telegram/");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botA, botB]));
        services.AddTelegramWebhooks(o => {
            o.BaseUri = new Uri("https://example.test/");
            o.SecretToken = "s3cr3t";
        });
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal("bot_a", botA.Username);
        Assert.Equal("bot_b", botB.Username);
        Assert.Equal(1, httpA.CallCount("setWebhook"));
        Assert.Equal(1, httpB.CallCount("setWebhook"));
    }

    // ---- polling dispatch / lifecycle --------------------------------------------------------------------------

    [Fact]
    public async Task TelegramPollingHostedService_StartAsync_ForwardsUpdateToDispatcher() {
        var (client, http) = TestBotClientFactory.Create();
        http.Enqueue("deleteWebhook", true);
        http.Enqueue("getMe", new User { Id = 1, IsBot = true, FirstName = "Bot", Username = "poll_bot" });
        var update = new Update { Id = 42, Message = new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } } };
        http.Enqueue("getUpdates", new[] { update });
        http.SetDefaultResponse("getUpdates", Array.Empty<Update>());

        var dispatcher = Substitute.For<IDispatcher>();
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("poll-bot");
        botData.Bot.Returns(client);
        botData.Dispatcher.Returns(dispatcher);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(CancellationToken.None);
        services.AddSingleton(lifetime);
        services.AddTelegramPolling();
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);
        try {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (dispatcher.ReceivedCalls().Count() == 0 && DateTime.UtcNow < deadline) {
                await Task.Delay(10, TestContext.Current.CancellationToken);
            }

            await dispatcher.Received(1).DispatchUpdateAsync(
                Arg.Is<Update>(u => u.Id == 42), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
        }
        finally {
            await hosted.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TelegramPollingHostedService_StopAsync_WithoutStartAsync_DoesNotThrow() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([]));
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(CancellationToken.None);
        services.AddSingleton(lifetime);
        services.AddTelegramPolling();
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StopAsync(TestContext.Current.CancellationToken);
    }

    // ---- startup newsletter -------------------------------------------------------------------------------------

    [Fact]
    public void AddStartupNewsletter_RegistersHostedService() {
        var services = new ServiceCollection();

        services.AddStartupNewsletter();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IHostedService));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal("StartupNewsletterHostedService", descriptor.ImplementationType!.Name);
    }

    [Fact]
    public async Task StartupNewsletterHostedService_NoStartupNewsletterConfigured_SendsNothing() {
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("bot-a");
        botData.StartupNewsletter.Returns((StartupNewsletter?) null);

        var newsletterService = Substitute.For<INewsletterService>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddScoped(_ => newsletterService);
        services.AddStartupNewsletter();
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);

        await newsletterService.DidNotReceive().GetNewsletterByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartupNewsletterHostedService_UnknownNewsletter_SkipsSendWithoutThrowing() {
        var startupNewsletter = new StartupNewsletter("missing", (_, _, _, _) => Task.CompletedTask);
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("bot-a");
        botData.StartupNewsletter.Returns(startupNewsletter);

        var newsletterService = Substitute.For<INewsletterService>();
        newsletterService.GetNewsletterByKeyAsync("missing", Arg.Any<CancellationToken>()).Returns((Newsletter?) null);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddScoped(_ => newsletterService);
        services.AddStartupNewsletter();
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);

        await newsletterService.DidNotReceive().SendNewsletterAsync(
            Arg.Any<string>(), Arg.Any<Func<TelegramChat, CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartupNewsletterHostedService_ExistingNewsletter_InvokesActionThroughSendNewsletter() {
        var invokedChats = new List<TelegramChat>();
        var startupNewsletter = new StartupNewsletter("news", (_, chat, _, _) => {
            invokedChats.Add(chat);
            return Task.CompletedTask;
        });
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns("bot-a");
        botData.StartupNewsletter.Returns(startupNewsletter);

        var newsletterService = Substitute.For<INewsletterService>();
        newsletterService.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        Func<TelegramChat, CancellationToken, Task>? capturedSend = null;
        newsletterService.SendNewsletterAsync(
                "news",
                Arg.Do<Func<TelegramChat, CancellationToken, Task>>(f => capturedSend = f),
                Arg.Any<CancellationToken>())
            .Returns(new SendResult(1, new Dictionary<TelegramChat, Exception>()));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddScoped(_ => newsletterService);
        services.AddStartupNewsletter();
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(capturedSend);
        var chat = new TelegramChat(5) { Type = ChatType.Private };
        await capturedSend!(chat, TestContext.Current.CancellationToken);

        Assert.Single(invokedChats);
        Assert.Same(chat, invokedChats[0]);
    }
}
