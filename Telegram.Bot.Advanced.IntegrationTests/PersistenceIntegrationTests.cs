using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.IntegrationTests.Infrastructure;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.IntegrationTests;

/// <summary>
/// Lane A: behaviours only a real relational provider enforces (composite primary keys, foreign keys, cascade
/// deletes) that the unit suite's InMemory provider does not. A temp SQLite database, no host.
/// </summary>
public sealed class PersistenceIntegrationTests {
    [Fact]
    public async Task Data_DuplicateUserIdAndKey_ViolatesCompositePrimaryKey() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);

        await using (var seed = db.CreateContext()) {
            var chat = new TelegramChat(1) { Type = ChatType.Private };
            seed.Users.Add(chat);
            seed.Data.Add(new Data(chat, "key", "value1"));
            await seed.SaveChangesAsync(ct);
        }

        // Loaded without Include(Data): the tracked graph does not already contain a conflicting Data entity, so
        // the duplicate key is caught by SQLite, not EF's local change tracker.
        await using var context = db.CreateContext();
        var chat2 = await context.Users.FirstAsync(u => u.Id == 1, ct);
        context.Data.Add(new Data(chat2, "key", "value2"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task NewsletterChat_DuplicateNewsletterKeyAndChatId_ViolatesCompositePrimaryKey() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);

        await using (var seed = db.CreateContext()) {
            var chat = new TelegramChat(1) { Type = ChatType.Private };
            var newsletter = new Newsletter("news", "desc");
            seed.Users.Add(chat);
            seed.Newsletters.Add(newsletter);
            seed.NewsletterChats.Add(new NewsletterChat(newsletter, chat));
            await seed.SaveChangesAsync(ct);
        }

        await using var context = db.CreateContext();
        var chat2 = await context.Users.FirstAsync(u => u.Id == 1, ct);
        var newsletter2 = await context.Newsletters.FirstAsync(n => n.Key == "news", ct);
        context.NewsletterChats.Add(new NewsletterChat(newsletter2, chat2));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task NewsletterChat_UnknownNewsletterKey_ViolatesForeignKey() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);
        await using var context = db.CreateContext();

        var chat = new TelegramChat(1) { Type = ChatType.Private };
        context.Users.Add(chat);
        context.NewsletterChats.Add(new NewsletterChat { NewsletterKey = "missing", ChatId = 1, Chat = chat });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task RemoveNewsletterAsync_WithSubscribers_CascadeDeletesJoinRows() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);

        await using (var seed = db.CreateContext()) {
            var chat = new TelegramChat(1) { Type = ChatType.Private };
            var newsletter = new Newsletter("news", "desc");
            seed.Users.Add(chat);
            seed.Newsletters.Add(newsletter);
            seed.NewsletterChats.Add(new NewsletterChat(newsletter, chat));
            await seed.SaveChangesAsync(ct);
        }

        await using (var context = db.CreateContext()) {
            var service = new NewsletterService<IntegrationTelegramContext>(
                context, NullLogger<NewsletterService<IntegrationTelegramContext>>.Instance);

            Assert.True(await service.RemoveNewsletterAsync("news", ct));
        }

        await using var verify = db.CreateContext();
        Assert.Empty(verify.NewsletterChats);
        Assert.NotNull(await TelegramChat.GetAsync(verify, 1, ct));
    }

    [Fact]
    public async Task TelegramChat_GetAsync_LoadsDataAndNewsletterNavigationsFromRealDatabase() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);

        await using (var seed = db.CreateContext()) {
            var chat = new TelegramChat(1) { Type = ChatType.Private };
            var newsletter = new Newsletter("news", "desc");
            seed.Users.Add(chat);
            seed.Newsletters.Add(newsletter);
            chat.AddData("greeting", "hello");
            seed.NewsletterChats.Add(new NewsletterChat(newsletter, chat));
            await seed.SaveChangesAsync(ct);
        }

        await using var context = db.CreateContext();
        var loaded = await TelegramChat.GetAsync(context, 1, ct);

        Assert.NotNull(loaded);
        Assert.Equal("hello", loaded!["greeting"]);
        var subscription = Assert.Single(loaded.NewsletterChats);
        Assert.Equal("news", subscription.Newsletter.Key);
    }

    [Fact]
    public async Task NewsletterService_GlobalSend_ReachesEveryPersistedChat() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);

        await using (var seed = db.CreateContext()) {
            seed.Users.Add(new TelegramChat(3) { Type = ChatType.Private });
            seed.Users.Add(new TelegramChat(1) { Type = ChatType.Private });
            seed.Users.Add(new TelegramChat(2) { Type = ChatType.Private });
            await seed.SaveChangesAsync(ct);
        }

        await using var context = db.CreateContext();
        var service = new NewsletterService<IntegrationTelegramContext>(
            context, NullLogger<NewsletterService<IntegrationTelegramContext>>.Instance);
        var visited = new List<long>();

        var result = await service.SendNewsletterAsync((chat, _) => {
            visited.Add(chat.Id);
            return Task.CompletedTask;
        }, ct);

        Assert.Equal(3, result.TotalSuccesses);
        Assert.Equal([1L, 2L, 3L], visited.Order());
    }

    [Fact]
    public async Task Dispatcher_WithSqliteContext_PersistsChatAndSenderRows() {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TempSqliteDatabase.CreateAsync(ct);

        var services = new ServiceCollection();
        services.AddLogging();
        db.AddTo(services);
        services.AddSingleton<ProbeRecorder>();

        var client = FakeTelegramApi.CreateClient(out var api);
        api.SetDefaultResponse("getChat", new ChatFullInfo { Id = 0, Type = ChatType.Private });
        api.SetDefaultResponse("sendMessage", new Message {
            Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 201, Type = ChatType.Private }
        });

        var botData = new TelegramBotData(o => {
            o.Endpoint = "sqlite-bot";
            o.Bot = client;
            o.UserUpdate = UserUpdate.EveryMessage;
            o.GroupChatBehaviour = IgnoreBehaviour.IgnoreNothing;
            o.PrivateChatBehaviour = IgnoreBehaviour.IgnoreNothing;
            o.DispatcherBuilder = new DispatcherBuilder<IntegrationTelegramContext, IntegrationProbeController>();
        });
        services.AddTelegramHolder(botData);

        var provider = services.BuildServiceProvider();

        var update = UpdateFactory.TextMessage(201, "/echo hi", senderId: 555);
        await botData.Dispatcher.DispatchUpdateAsync(update, provider, ct);

        await using var context = db.CreateContext();
        Assert.NotNull(await TelegramChat.GetAsync(context, 201, ct));
        Assert.NotNull(await context.Users.FirstOrDefaultAsync(u => u.Id == 555, ct));
    }
}
