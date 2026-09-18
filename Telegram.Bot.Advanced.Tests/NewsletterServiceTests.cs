using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Advanced.Tests.Infrastructure;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Tests;

/// <summary>
/// Covers <see cref="NewsletterService{TDbContext}"/> subscribe/unsubscribe composite-key semantics, missing-entity
/// diagnostics, sequential send totals, and <see cref="SendResult"/>'s defensive copy / cancellation propagation.
/// </summary>
public sealed class NewsletterServiceTests {
    private static NewsletterService<TestTelegramContext> CreateService(TestTelegramContext context) =>
        new(context, Substitute.For<ILogger<NewsletterService<TestTelegramContext>>>());

    private static async Task SeedAsync(TestTelegramContext context, string newsletterKey, params long[] chatIds) {
        var ct = TestContext.Current.CancellationToken;
        context.Newsletters.Add(new Newsletter(newsletterKey, "description"));
        foreach (var chatId in chatIds) {
            context.Users.Add(new TelegramChat(chatId) { Type = ChatType.Private });
        }
        await context.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task SubscribeChatAsync_UnsubscribeChatAsync_CompositeKeyRoundTrip() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        await SeedAsync(context, "news", 1);
        var service = CreateService(context);

        var subscribed = await service.SubscribeChatAsync("news", 1, ct);
        Assert.True(subscribed);
        var entry = await context.NewsletterChats.FindAsync([ "news", 1L ], ct);
        Assert.NotNull(entry);

        var unsubscribed = await service.UnsubscribeChatAsync("news", 1, ct);
        Assert.True(unsubscribed);
        Assert.Null(await context.NewsletterChats.FindAsync(["news", 1L], ct));
    }

    [Fact]
    public async Task SubscribeChatAsync_UnknownNewsletter_ThrowsNewsletterExceptionContainingKey() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        context.Users.Add(new TelegramChat(1) { Type = ChatType.Private });
        await context.SaveChangesAsync(ct);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<NewsletterException>(() => service.SubscribeChatAsync("missing-key", 1, ct));

        Assert.Contains("missing-key", exception.Message);
    }

    [Fact]
    public async Task SubscribeChatAsync_UnknownChat_ThrowsNewsletterExceptionContainingChatId() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        context.Newsletters.Add(new Newsletter("news", "description"));
        await context.SaveChangesAsync(ct);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<NewsletterException>(() => service.SubscribeChatAsync("news", 12345, ct));

        Assert.Contains("12345", exception.Message);
    }

    [Fact]
    public async Task UnsubscribeChatAsync_UnknownNewsletter_ThrowsNewsletterExceptionContainingKey() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        context.Users.Add(new TelegramChat(1) { Type = ChatType.Private });
        await context.SaveChangesAsync(ct);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<NewsletterException>(() => service.UnsubscribeChatAsync("missing-key", 1, ct));

        Assert.Contains("missing-key", exception.Message);
    }

    [Fact]
    public async Task SubscribeChatAsync_AlreadySubscribed_ReturnsFalse() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        await SeedAsync(context, "news", 1);
        var service = CreateService(context);

        Assert.True(await service.SubscribeChatAsync("news", 1, ct));
        Assert.False(await service.SubscribeChatAsync("news", 1, ct));
    }

    [Fact]
    public async Task UnsubscribeChatAsync_NotSubscribed_ReturnsFalse() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        await SeedAsync(context, "news", 1);
        var service = CreateService(context);

        Assert.False(await service.UnsubscribeChatAsync("news", 1, ct));
    }

    [Fact]
    public async Task SendNewsletterAsync_MixedOutcomes_TotalsReflectSuccessesAndErrorsSequentially() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        await SeedAsync(context, "news", 1, 2, 3);
        var service = CreateService(context);
        await service.SubscribeChatAsync("news", 1, ct);
        await service.SubscribeChatAsync("news", 2, ct);
        await service.SubscribeChatAsync("news", 3, ct);

        var failure = new InvalidOperationException("send failed for chat 2");
        var visited = new List<long>();
        Task SendAction(TelegramChat chat, CancellationToken token) {
            visited.Add(chat.Id);
            if (chat.Id == 2) throw failure;
            return Task.CompletedTask;
        }

        var result = await service.SendNewsletterAsync("news", SendAction, ct);

        Assert.Equal([1L, 2L, 3L], visited);
        Assert.Equal(2, result.TotalSuccesses);
        Assert.Equal(1, result.TotalErrors);
        Assert.Equal(3, result.TotalSubscribers);
        var failedChat = Assert.Single(result.Errors.Keys);
        Assert.Equal(2, failedChat.Id);
        Assert.Same(failure, result.Errors[failedChat]);
    }

    [Fact]
    public void SendResult_Constructor_ErrorsIsDefensiveCopyIndependentOfSourceDictionary() {
        var chat = new TelegramChat(1) { Type = ChatType.Private };
        var source = new Dictionary<TelegramChat, Exception> { [chat] = new InvalidOperationException("boom") };

        var result = new SendResult(0, source);
        source[new TelegramChat(2) { Type = ChatType.Private }] = new InvalidOperationException("added-after-construction");

        Assert.Single(result.Errors);
        Assert.Equal(1, result.TotalErrors);
    }

    [Fact]
    public async Task SendNewsletterAsync_SendActionThrowsOperationCanceled_PropagatesAndIsNotRecordedAsError() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        await SeedAsync(context, "news", 1, 2);
        var service = CreateService(context);
        await service.SubscribeChatAsync("news", 1, ct);
        await service.SubscribeChatAsync("news", 2, ct);

        Task SendAction(TelegramChat chat, CancellationToken token) => throw new OperationCanceledException("cancelled mid-send");

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SendNewsletterAsync("news", SendAction, ct));
    }

    // ---- global send, missing-newsletter overload, subscription queries, CRUD, DI registration ---------------

    [Fact]
    public async Task SendNewsletterAsync_Global_VisitsEveryChatRegardlessOfSubscription() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        await SeedAsync(context, "news", 1, 2, 3);
        var service = CreateService(context);
        await service.SubscribeChatAsync("news", 1, ct);

        var visited = new List<long>();
        Task SendAction(TelegramChat chat, CancellationToken token) { visited.Add(chat.Id); return Task.CompletedTask; }

        var result = await service.SendNewsletterAsync(SendAction, ct);

        Assert.Equal([1L, 2L, 3L], visited);
        Assert.Equal(3, result.TotalSuccesses);
    }

    [Fact]
    public async Task SendNewsletterAsync_NamedKeyWithoutNewsletterEntity_SendsToNobodyWithoutThrowing() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        var service = CreateService(context);
        var called = false;
        Task SendAction(TelegramChat chat, CancellationToken token) { called = true; return Task.CompletedTask; }

        var result = await service.SendNewsletterAsync("nonexistent-key", SendAction, ct);

        Assert.False(called);
        Assert.Equal(0, result.TotalSuccesses);
        Assert.Equal(0, result.TotalErrors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsChatSubscribedToNewsletterAsync_ReflectsSubscriptionState(bool subscribed) {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        await SeedAsync(context, "news", 1);
        var service = CreateService(context);
        if (subscribed) {
            await service.SubscribeChatAsync("news", 1, ct);
        }

        Assert.Equal(subscribed, await service.IsChatSubscribedToNewsletterAsync("news", 1, ct));
    }

    [Fact]
    public async Task IsChatSubscribedToNewsletterAsync_UnknownNewsletter_ThrowsNewsletterException() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        context.Users.Add(new TelegramChat(1) { Type = ChatType.Private });
        await context.SaveChangesAsync(ct);
        var service = CreateService(context);

        await Assert.ThrowsAsync<NewsletterException>(() => service.IsChatSubscribedToNewsletterAsync("missing-key", 1, ct));
    }

    [Fact]
    public async Task IsChatSubscribedToNewsletterAsync_UnknownChat_ThrowsNewsletterException() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        context.Newsletters.Add(new Newsletter("news", "description"));
        await context.SaveChangesAsync(ct);
        var service = CreateService(context);

        await Assert.ThrowsAsync<NewsletterException>(() => service.IsChatSubscribedToNewsletterAsync("news", 12345, ct));
    }

    [Fact]
    public async Task CreateNewsletterAsync_NewKey_ReturnsTrueAndPersists() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        var service = CreateService(context);

        var created = await service.CreateNewsletterAsync(new Newsletter("news", "desc"), ct);

        Assert.True(created);
        Assert.NotNull(await context.Newsletters.FindAsync(["news"], ct));
    }

    [Fact]
    public async Task CreateNewsletterAsync_ExistingKey_ReturnsFalseWithoutOverwritingDescription() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        var service = CreateService(context);
        await service.CreateNewsletterAsync(new Newsletter("news", "original"), ct);

        var created = await service.CreateNewsletterAsync(new Newsletter("news", "replacement"), ct);

        Assert.False(created);
        var persisted = await context.Newsletters.FindAsync(["news"], ct);
        Assert.Equal("original", persisted!.Description);
    }

    [Fact]
    public async Task RemoveNewsletterAsync_ExistingKey_ReturnsTrueAndDeletes() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        var service = CreateService(context);
        await service.CreateNewsletterAsync(new Newsletter("news", "desc"), ct);

        var removed = await service.RemoveNewsletterAsync("news", ct);

        Assert.True(removed);
        Assert.Null(await context.Newsletters.FindAsync(["news"], ct));
    }

    [Fact]
    public async Task RemoveNewsletterAsync_MissingKey_ReturnsFalse() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        var service = CreateService(context);

        Assert.False(await service.RemoveNewsletterAsync("missing", ct));
    }

    [Fact]
    public async Task GetNewslettersAsync_ReturnsAllPersistedNewsletters() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        var service = CreateService(context);
        await service.CreateNewsletterAsync(new Newsletter("news1", "d1"), ct);
        await service.CreateNewsletterAsync(new Newsletter("news2", "d2"), ct);

        var newsletters = await service.GetNewslettersAsync(ct);

        Assert.Equal(["news1", "news2"], newsletters.Select(n => n.Key).OrderBy(k => k));
    }

    [Fact]
    public async Task GetNewsletterByKeyAsync_MissingKey_ReturnsNull() {
        var ct = TestContext.Current.CancellationToken;
        await using var context = TestTelegramContext.Create();
        var service = CreateService(context);

        Assert.Null(await service.GetNewsletterByKeyAsync("missing", ct));
    }

    [Fact]
    public void AddNewsletter_RegistersScopedNewsletterService() {
        var services = new ServiceCollection();

        services.AddNewsletter<TestTelegramContext>();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(INewsletterService));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(NewsletterService<TestTelegramContext>), descriptor.ImplementationType);
    }
}
