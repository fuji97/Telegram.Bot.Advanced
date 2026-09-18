using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.IntegrationTests.Infrastructure;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.IntegrationTests;

/// <summary>
/// Lane B: exercises the real Telegram Bot API with a user-supplied token (<c>TBA_IT_BOT_TOKEN</c>). No mocking:
/// every bot client here is real. Self-skips with an actionable message when the token (or, for chat-scoped
/// tests, <c>TBA_IT_CHAT_ID</c>) is absent.
/// </summary>
public sealed class LiveTelegramApiTests {
    /// <summary>
    /// Minimal <see cref="IHostApplicationLifetime"/> for hosted-service tests run outside a real <c>IHost</c>.
    /// </summary>
    private sealed class ManualHostLifetime : IHostApplicationLifetime {
        private readonly CancellationTokenSource _stopping = new();
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => _stopping.Token;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() => _stopping.Cancel();
    }

    [Fact]
    public async Task GetMe_WithSuppliedToken_ReturnsBotAccount() {
        var ct = TestContext.Current.CancellationToken;
        var token = IntegrationSettings.RequireToken();
        var bot = new TelegramBotClient(token);

        var me = await bot.GetMe(ct);

        Assert.True(me.IsBot);
        Assert.False(string.IsNullOrWhiteSpace(me.Username));
    }

    [Fact]
    public async Task PollingHostedService_WithRealToken_ResolvesUsernameAndClearsWebhook() {
        var ct = TestContext.Current.CancellationToken;
        var token = IntegrationSettings.RequireToken();
        var bot = new TelegramBotClient(token);
        var me = await bot.GetMe(ct);

        var botData = new TelegramBotData(o => {
            o.Endpoint = "live-poll-bot";
            o.Bot = bot;
            o.DispatcherBuilder = new DispatcherBuilder<IntegrationTelegramContext, IntegrationProbeController>();
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelegramHolder>(new TelegramHolder([botData]));
        services.AddSingleton<IHostApplicationLifetime>(new ManualHostLifetime());
        services.AddTelegramPolling();
        var provider = services.BuildServiceProvider();
        var hosted = Assert.Single(provider.GetServices<IHostedService>());

        await hosted.StartAsync(ct);
        try {
            Assert.Equal(me.Username, botData.Username);

            var info = await bot.GetWebhookInfo(ct);
            Assert.True(string.IsNullOrEmpty(info.Url));
        }
        finally {
            await hosted.StopAsync(ct);
        }
    }

    [Fact]
    public async Task Dispatch_RealClientAndTempDatabase_DeliversReplyAndPersistsChat() {
        var ct = TestContext.Current.CancellationToken;
        var token = IntegrationSettings.RequireToken();
        var chatId = IntegrationSettings.RequireChatId();
        var bot = new TelegramBotClient(token);

        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        var services = new ServiceCollection();
        services.AddLogging();
        db.AddTo(services);
        services.AddSingleton<ProbeRecorder>();

        var botData = new TelegramBotData(o => {
            o.Endpoint = "live-dispatch-bot";
            o.Bot = bot;
            o.UserUpdate = UserUpdate.EveryMessage;
            o.GroupChatBehaviour = IgnoreBehaviour.IgnoreNothing;
            o.PrivateChatBehaviour = IgnoreBehaviour.IgnoreNothing;
            o.DispatcherBuilder = new DispatcherBuilder<IntegrationTelegramContext, IntegrationProbeController>();
        });
        services.AddTelegramHolder(botData);
        var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<ProbeRecorder>();

        Telegram.Bot.Types.Message? sent = null;
        try {
            var update = UpdateFactory.TextMessage(chatId, "/echo hello", senderId: chatId);
            await botData.Dispatcher.DispatchUpdateAsync(update, provider, ct);

            Assert.True(recorder.SentMessages.TryDequeue(out sent));
            Assert.Equal("echo:hello", sent!.Text);
            Assert.Equal(chatId, sent.Chat.Id);

            await using var context = db.CreateContext();
            var chat = await TelegramChat.GetAsync(context, chatId, ct);
            Assert.NotNull(chat);

            var realChat = await bot.GetChat(chatId, ct);
            Assert.Equal(realChat.Type, chat!.Type);
        }
        finally {
            if (sent is not null) {
                try {
                    await bot.DeleteMessage(chatId, sent.Id, ct);
                }
                catch (ApiRequestException) {
                    // Best-effort cleanup only; a message the bot can no longer see is not a test failure.
                }
            }
        }
    }

    [Fact]
    public async Task Newsletter_RealSend_AggregatesPerRecipientErrors() {
        var ct = TestContext.Current.CancellationToken;
        var token = IntegrationSettings.RequireToken();
        var chatId = IntegrationSettings.RequireChatId();
        var bot = new TelegramBotClient(token);

        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using (var seed = db.CreateContext()) {
            var newsletter = new Newsletter("it-news", "Integration test newsletter");
            var realChat = new TelegramChat(chatId) { Type = ChatType.Private };
            var bogusChat = new TelegramChat(1) { Type = ChatType.Private };
            seed.Newsletters.Add(newsletter);
            seed.Users.Add(realChat);
            seed.Users.Add(bogusChat);
            seed.NewsletterChats.Add(new NewsletterChat(newsletter, realChat));
            seed.NewsletterChats.Add(new NewsletterChat(newsletter, bogusChat));
            await seed.SaveChangesAsync(ct);
        }

        await using var context = db.CreateContext();
        var service = new NewsletterService<IntegrationTelegramContext>(
            context, NullLogger<NewsletterService<IntegrationTelegramContext>>.Instance);

        int? deliveredMessageId = null;
        var result = await service.SendNewsletterAsync("it-news", async (chat, token2) => {
            var message = await bot.SendMessage(chat.ToChatId(), "integration newsletter", cancellationToken: token2);
            if (chat.Id == chatId) {
                deliveredMessageId = message.Id;
            }
        }, ct);

        try {
            Assert.Equal(1, result.TotalSuccesses);
            Assert.Equal(1, result.TotalErrors);
            var error = Assert.Single(result.Errors);
            Assert.Equal(1, error.Key.Id);
            Assert.IsType<ApiRequestException>(error.Value);
        }
        finally {
            if (deliveredMessageId is { } id) {
                try {
                    await bot.DeleteMessage(chatId, id, ct);
                }
                catch (ApiRequestException) {
                    // Best-effort cleanup only.
                }
            }
        }
    }
}
