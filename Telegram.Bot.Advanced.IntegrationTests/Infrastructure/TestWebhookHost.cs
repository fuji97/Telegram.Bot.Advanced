using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.IntegrationTests.Infrastructure;

/// <summary>
/// A real Kestrel host wired exactly the way a hosting application would: <see cref="IntegrationTelegramContext"/>
/// over a real SQLite file, the real dispatcher/webhook pipeline, and either a real <see cref="TelegramBotClient"/>
/// (Lane C) or a <see cref="FakeTelegramApi"/>-backed one (Lane A).
/// </summary>
public sealed class TestWebhookHost : IAsyncDisposable {
    public WebApplication App { get; }
    public Uri BaseAddress { get; }
    public HttpClient Http { get; }
    public FakeTelegramApi? Api { get; }
    public ProbeRecorder Recorder { get; }
    public string WebhookPath { get; }
    public ITelegramBotClient Bot { get; }

    private TestWebhookHost(
        WebApplication app,
        Uri baseAddress,
        HttpClient http,
        FakeTelegramApi? api,
        ProbeRecorder recorder,
        string webhookPath,
        ITelegramBotClient bot) {
        App = app;
        BaseAddress = baseAddress;
        Http = http;
        Api = api;
        Recorder = recorder;
        WebhookPath = webhookPath;
        Bot = bot;
    }

    public static async Task<TestWebhookHost> StartAsync(
        TempSqliteDatabase db, TestWebhookHostOptions options, CancellationToken cancellationToken) {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://127.0.0.1:{options.Port}");

        db.AddTo(builder.Services);
        builder.Services.AddSingleton<ProbeRecorder>();
        builder.Services.AddNewsletter<IntegrationTelegramContext>();

        FakeTelegramApi? api = null;
        var client = options.RealBotToken is { } token ? new TelegramBotClient(token) : FakeTelegramApi.CreateClient(out api);

        var botData = new TelegramBotData(o => {
            o.Endpoint = options.Endpoint;
            o.BasePath = options.BasePath;
            o.Bot = client;
            o.UserUpdate = UserUpdate.EveryMessage;
            o.GroupChatBehaviour = IgnoreBehaviour.IgnoreNothing;
            o.PrivateChatBehaviour = IgnoreBehaviour.IgnoreNothing;
            o.DispatcherBuilder = new DispatcherBuilder<IntegrationTelegramContext, IntegrationProbeController>();
        });

        builder.Services.AddTelegramHolder(botData);

        if (options.RegisterWebhookTransport) {
            builder.Services.AddTelegramWebhooks(o => {
                o.BaseUri = options.PublicBaseUri ?? new Uri($"http://127.0.0.1:{options.Port}/");
                o.SecretToken = options.SecretToken;
                o.DeleteWebhookOnShutdown = options.DeleteWebhookOnShutdown;
            });

            if (api is not null) {
                api.Enqueue("getMe", new User { Id = 42, IsBot = true, FirstName = "IT Bot", Username = "it_bot" });
                api.Enqueue("setWebhook", true);
                api.Enqueue("deleteWebhook", true);
                api.SetDefaultResponse("getChat", new ChatFullInfo { Id = 0, Type = ChatType.Private });
                api.SetDefaultResponse("sendMessage", new Message {
                    Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }
                });
            }
        }

        var app = builder.Build();
        app.MapTelegramWebhooks();
        await app.StartAsync(cancellationToken);

        var addressesFeature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        var baseAddress = new Uri(addressesFeature.Addresses.First());
        var httpClient = new HttpClient { BaseAddress = baseAddress };
        var recorder = app.Services.GetRequiredService<ProbeRecorder>();
        var webhookPath = $"{options.BasePath}{options.Endpoint}";

        return new TestWebhookHost(app, baseAddress, httpClient, api, recorder, webhookPath, client);
    }

    public async Task<HttpResponseMessage> PostUpdateAsync(
        Update update, string? secret, Uri? target = null, CancellationToken cancellationToken = default) {
        using var request = new HttpRequestMessage(HttpMethod.Post, target ?? new Uri(BaseAddress, WebhookPath)) {
            Content = new StringContent(UpdateFactory.Serialize(update), Encoding.UTF8, "application/json")
        };
        if (secret is not null) {
            request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", secret);
        }

        return await Http.SendAsync(request, cancellationToken);
    }

    public async ValueTask DisposeAsync() {
        await App.StopAsync();
        await App.DisposeAsync();
        Http.Dispose();
    }
}
