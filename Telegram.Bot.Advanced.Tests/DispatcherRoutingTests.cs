using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Tests.Infrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Tests;

/// <summary>
/// Exercises <see cref="Dispatcher{TContext}"/> end to end: handler selection, filter override points, return-kind
/// awaiting, cancellation propagation, exception unwrapping, retry/dedup, per-bot targeting, scope disposal, and
/// chat-creation/GetChat call counting.
/// </summary>
public sealed class DispatcherRoutingTests {
    // ---- shared test-only filters -----------------------------------------------------------------------------

    private sealed class ConstantFilter(bool result) : DispatcherFilterAttribute {
        public override bool IsValid(Update update, TelegramChat? chat, MessageCommand command, ITelegramBotData botData) => result;
    }

    /// <summary>
    /// Overrides only IsValidController (always false); IsValidMethod keeps the base implementation (delegates to
    /// IsValid, which is always true). Applying this to a class vs. a method proves the two override points are
    /// genuinely separate dispatch checks.
    /// </summary>
    private sealed class RejectAtControllerLevelOnlyFilter : DispatcherFilterAttribute {
        public override bool IsValid(Update update, TelegramChat? chat, MessageCommand command, ITelegramBotData botData) => true;
        public override bool IsValidController(Update update, TelegramChat? chat, MessageCommand command, ITelegramBotData botData) => false;
    }

    // ---- shared test-only controllers --------------------------------------------------------------------------

    [ConstantFilter(false)]
    private sealed class NeverMatchController : TelegramController<TestTelegramContext> {
        public static bool Called;
        public Task Handle() { Called = true; return Task.CompletedTask; }
    }

    private sealed class FallbackOnlyController : TelegramController<TestTelegramContext> {
        public static int CallCount;

        [ConstantFilter(false)]
        public Task RegularHandler() => Task.CompletedTask;

        [NoMethodFilter]
        public Task Fallback() { CallCount++; return Task.CompletedTask; }
    }

    [RejectAtControllerLevelOnlyFilter]
    private sealed class ControllerLevelRejectController : TelegramController<TestTelegramContext> {
        public static bool Called;
        public Task Handle() { Called = true; return Task.CompletedTask; }
    }

    private sealed class MethodLevelAcceptController : TelegramController<TestTelegramContext> {
        public static bool Called;

        [RejectAtControllerLevelOnlyFilter]
        public Task Handle() { Called = true; return Task.CompletedTask; }
    }

    private sealed class TaskHandlerController : TelegramController<TestTelegramContext> {
        public static bool CompletedAfterYield;

        public async Task Handle() {
            await Task.Yield();
            CompletedAfterYield = true;
        }
    }

    private sealed class ValueTaskHandlerController : TelegramController<TestTelegramContext> {
        public static bool CompletedAfterYield;

        public async ValueTask Handle() {
            await Task.Yield();
            CompletedAfterYield = true;
        }
    }

    private sealed class TaskOfIntHandlerController : TelegramController<TestTelegramContext> {
        public Task<int> Handle() => Task.FromResult(1);
    }

    private sealed class CancellationCapturingController : TelegramController<TestTelegramContext> {
        public static CancellationToken? Captured;

        public Task Handle() {
            Captured = CancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandlerController : TelegramController<TestTelegramContext> {
        public sealed class BoomException() : Exception("boom-from-handler");

        public Task Handle() => throw new BoomException();
    }

    private sealed class CountingController : TelegramController<TestTelegramContext> {
        public static int CallCount;
        public Task Handle() { CallCount++; return Task.CompletedTask; }
    }

    private sealed class DisposalTracker : IAsyncDisposable {
        public bool Disposed { get; private set; }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }

    private sealed class ScopeDisposalController : TelegramController<TestTelegramContext> {
        public static DisposalTracker? CapturedTracker;

        public ScopeDisposalController(DisposalTracker tracker) {
            CapturedTracker = tracker;
        }

        public Task Handle() => Task.CompletedTask;
    }

    private static Update TextMessageUpdate(long chatId, string text, int updateId = 1) => new() {
        Id = updateId,
        Message = new Message {
            Id = updateId,
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = chatId, Type = ChatType.Private },
            From = new User { Id = chatId, FirstName = "User" },
            Text = text
        }
    };

    /// <summary>
    /// Every dispatch of a message for a chat id not yet in the database triggers exactly one GetChat call
    /// (see Dispatcher.UpdateChat); queue its canned response so the real TelegramBotClient doesn't fail the test.
    /// </summary>
    private static void EnqueueChatCreation(DispatcherTestHarness.Harness harness, long chatId) =>
        harness.HttpHandler.Enqueue("getChat", new ChatFullInfo { Id = chatId, Type = ChatType.Private, Description = "desc" });

    // ---- tests --------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Dispatch_NoHandlerMatchesAndNoFallback_CompletesWithoutExceptionOrHandlerCall() {
        var harness = DispatcherTestHarness.Create([typeof(NeverMatchController)]);
        EnqueueChatCreation(harness, 1);
        NeverMatchController.Called = false;

        var exception = await Record.ExceptionAsync(() =>
            harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken));

        Assert.Null(exception);
        Assert.False(NeverMatchController.Called);
    }

    [Fact]
    public async Task Dispatch_NoRegularHandlerMatches_FallbackHandlerInvokedExactlyOnce() {
        var harness = DispatcherTestHarness.Create([typeof(FallbackOnlyController)]);
        EnqueueChatCreation(harness, 1);
        FallbackOnlyController.CallCount = 0;

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken);

        Assert.Equal(1, FallbackOnlyController.CallCount);
    }

    [Fact]
    public async Task Dispatch_FilterAppliedAtControllerLevel_UsesIsValidControllerNotIsValidMethod() {
        var harness = DispatcherTestHarness.Create([typeof(ControllerLevelRejectController), typeof(MethodLevelAcceptController)]);
        EnqueueChatCreation(harness, 1);
        ControllerLevelRejectController.Called = false;
        MethodLevelAcceptController.Called = false;

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken);

        Assert.False(ControllerLevelRejectController.Called, "A controller-level filter rejection must block dispatch via IsValidController.");
        Assert.True(MethodLevelAcceptController.Called, "The same filter type at method level must be checked via IsValidMethod, not IsValidController.");
    }

    [Fact]
    public async Task Dispatch_TaskReturningHandler_IsAwaitedToCompletion() {
        var harness = DispatcherTestHarness.Create([typeof(TaskHandlerController)]);
        EnqueueChatCreation(harness, 1);
        TaskHandlerController.CompletedAfterYield = false;

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken);

        Assert.True(TaskHandlerController.CompletedAfterYield);
    }

    [Fact]
    public async Task Dispatch_ValueTaskReturningHandler_IsAwaitedToCompletion() {
        var harness = DispatcherTestHarness.Create([typeof(ValueTaskHandlerController)]);
        EnqueueChatCreation(harness, 1);
        ValueTaskHandlerController.CompletedAfterYield = false;

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken);

        Assert.True(ValueTaskHandlerController.CompletedAfterYield);
    }

    [Fact]
    public void Constructor_ControllerHandlerReturnsTaskOfT_ThrowsInvalidControllerException() {
        var botData = NSubstitute.Substitute.For<ITelegramBotData>();

        Assert.Throws<InvalidControllerException>(() =>
            new Dispatcher<TestTelegramContext>(botData, [typeof(TaskOfIntHandlerController)]));
    }

    [Fact]
    public async Task Dispatch_PassesCancellationTokenToHandler() {
        var harness = DispatcherTestHarness.Create([typeof(CancellationCapturingController)]);
        EnqueueChatCreation(harness, 1);
        CancellationCapturingController.Captured = null;
        using var cts = new CancellationTokenSource();

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, cts.Token);

        Assert.Equal(cts.Token, CancellationCapturingController.Captured);
    }

    [Fact]
    public async Task Dispatch_CancelledToken_ThrowsOperationCanceledExceptionFromEfCall() {
        // No controllers are needed: cancellation must be observed inside the dispatcher's own EF calls
        // (chat lookup / SaveChangesAsync), before any handler selection - and before the GetChat network call for
        // chat creation - would even occur.
        var harness = DispatcherTestHarness.Create([]);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, cts.Token));

        Assert.Equal(0, harness.HttpHandler.CallCount("getChat"));
    }

    [Fact]
    public async Task Dispatch_HandlerThrows_ExceptionUnwrappedFromTargetInvocationException() {
        var harness = DispatcherTestHarness.Create([typeof(ThrowingHandlerController)]);
        EnqueueChatCreation(harness, 1);

        var exception = await Assert.ThrowsAsync<ThrowingHandlerController.BoomException>(() =>
            harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken));

        Assert.Equal("boom-from-handler", exception.Message);
    }

    [Fact]
    public async Task Dispatch_SameUpdateIdDispatchedTwice_SecondDispatchInvokesHandlerAgain() {
        var harness = DispatcherTestHarness.Create([typeof(CountingController)]);
        EnqueueChatCreation(harness, 1);
        CountingController.CallCount = 0;
        var update = TextMessageUpdate(1, "hi", updateId: 99);

        await harness.Dispatcher.DispatchUpdateAsync(update, harness.Provider, TestContext.Current.CancellationToken);
        await harness.Dispatcher.DispatchUpdateAsync(update, harness.Provider, TestContext.Current.CancellationToken);

        Assert.Equal(2, CountingController.CallCount);
    }

    [Fact]
    public async Task Dispatch_CommandTargetsDifferentBotUsername_HandlerNotCalledAndNoEfWrite() {
        var harness = DispatcherTestHarness.Create([typeof(CountingController)], username: "my_bot");
        CountingController.CallCount = 0;

        await harness.Dispatcher.DispatchUpdateAsync(
            TextMessageUpdate(1, "/handle@other_bot"), harness.Provider, TestContext.Current.CancellationToken);

        Assert.Equal(0, CountingController.CallCount);
        Assert.Equal(0, harness.HttpHandler.CallCount("getChat"));
        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        Assert.Equal(0, await context.Users.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Dispatch_CommandTargetsSameUsernameDifferentCase_HandlerIsCalled() {
        var harness = DispatcherTestHarness.Create([typeof(CountingController)], username: "My_Bot");
        EnqueueChatCreation(harness, 1);
        CountingController.CallCount = 0;

        await harness.Dispatcher.DispatchUpdateAsync(
            TextMessageUpdate(1, "/handle@my_bot"), harness.Provider, TestContext.Current.CancellationToken);

        Assert.Equal(1, CountingController.CallCount);
    }

    [Fact]
    public async Task Dispatch_PerCallScope_DisposedAfterDispatchCompletes() {
        var harness = DispatcherTestHarness.Create([typeof(ScopeDisposalController)]);
        EnqueueChatCreation(harness, 1);
        ScopeDisposalController.CapturedTracker = null;
        var services = new ServiceCollection();
        services.AddDbContext<TestTelegramContext>(o => o.UseInMemoryDatabase(harness.DatabaseName));
        services.AddScoped<DisposalTracker>();
        harness.Dispatcher.RegisterController(services);
        var provider = services.BuildServiceProvider();

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), provider, TestContext.Current.CancellationToken);

        Assert.NotNull(ScopeDisposalController.CapturedTracker);
        Assert.True(ScopeDisposalController.CapturedTracker!.Disposed);
    }

    [Fact]
    public async Task Dispatch_NewChat_FetchesChatFullInfoExactlyOnce() {
        var harness = DispatcherTestHarness.Create([]);
        EnqueueChatCreation(harness, 1);

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken);

        Assert.Equal(1, harness.HttpHandler.CallCount("getChat"));
    }

    [Fact]
    public async Task Dispatch_ExistingChat_DoesNotCallGetChatAgain() {
        var harness = DispatcherTestHarness.Create([]);
        EnqueueChatCreation(harness, 1);

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi", updateId: 1), harness.Provider, TestContext.Current.CancellationToken);
        Assert.Equal(1, harness.HttpHandler.CallCount("getChat"));

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi again", updateId: 2), harness.Provider, TestContext.Current.CancellationToken);

        Assert.Equal(1, harness.HttpHandler.CallCount("getChat"));
    }
}
