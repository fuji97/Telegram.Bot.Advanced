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
using Telegram.Bot.Advanced.Extensions;
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
}
