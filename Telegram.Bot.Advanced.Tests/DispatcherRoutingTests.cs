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

    // ---- ignore-behaviour / UserUpdate / chat persistence gaps ---------------------------------------------

    private sealed class IgnoreBehaviourProbeController : TelegramController<TestTelegramContext> {
        public static int CallCount;

        [NoMethodFilter]
        public Task Handle() { CallCount++; return Task.CompletedTask; }
    }

    private sealed class VoidHandlerController : TelegramController<TestTelegramContext> {
        public static bool Called;
        public void Handle() { Called = true; }
    }

    private sealed class PrePersistenceProbeController : TelegramController<TestTelegramContext> {
        public static bool? ChatExistedWhenHandlerRan;

        public async Task Handle() {
            ChatExistedWhenHandlerRan = await TelegramContext.Users.AsNoTracking().AnyAsync(u => u.Id == TelegramChat!.Id);
        }
    }

    private sealed class MessageCommandCapturingFallbackController : TelegramController<TestTelegramContext> {
        public static MessageCommand? Captured;

        [NoMethodFilter]
        public Task Handle() { Captured = MessageCommand; return Task.CompletedTask; }
    }

    private static Update ChatMessageUpdate(long chatId, ChatType chatType, string text, int updateId = 1) => new() {
        Id = updateId,
        Message = new Message {
            Id = updateId,
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = chatId, Type = chatType },
            From = new User { Id = chatId, FirstName = "User" },
            Text = text
        }
    };

    public static IEnumerable<object[]> IgnoreBehaviourScenarios() {
        (ChatType ChatType, IgnoreBehaviour Behaviour, string Text, bool Expected)[] rows = [
            (ChatType.Private, IgnoreBehaviour.IgnoreNothing, "plain text", true),
            (ChatType.Private, IgnoreBehaviour.IgnoreNothing, "/cmd", true),
            (ChatType.Private, IgnoreBehaviour.IgnoreNothing, "/cmd@test_bot", true),
            (ChatType.Group, IgnoreBehaviour.IgnoreNothing, "plain text", true),
            (ChatType.Group, IgnoreBehaviour.IgnoreNothing, "/cmd", true),
            (ChatType.Group, IgnoreBehaviour.IgnoreNothing, "/cmd@test_bot", true),
            (ChatType.Private, IgnoreBehaviour.IgnoreNonCommandMessages, "plain text", false),
            (ChatType.Private, IgnoreBehaviour.IgnoreNonCommandMessages, "/cmd", true),
            (ChatType.Private, IgnoreBehaviour.IgnoreNonCommandMessages, "/cmd@test_bot", true),
            (ChatType.Group, IgnoreBehaviour.IgnoreNonCommandMessages, "plain text", false),
            (ChatType.Group, IgnoreBehaviour.IgnoreNonCommandMessages, "/cmd", true),
            (ChatType.Group, IgnoreBehaviour.IgnoreNonCommandMessages, "/cmd@test_bot", true),
            (ChatType.Private, IgnoreBehaviour.IgnoreAllMessages, "plain text", false),
            (ChatType.Private, IgnoreBehaviour.IgnoreAllMessages, "/cmd", false),
            (ChatType.Private, IgnoreBehaviour.IgnoreAllMessages, "/cmd@test_bot", false),
            (ChatType.Group, IgnoreBehaviour.IgnoreAllMessages, "plain text", false),
            (ChatType.Group, IgnoreBehaviour.IgnoreAllMessages, "/cmd", false),
            (ChatType.Group, IgnoreBehaviour.IgnoreAllMessages, "/cmd@test_bot", false),
            (ChatType.Private, IgnoreBehaviour.IgnoreAllMessagesAndCommandsWithoutTarget, "plain text", false),
            (ChatType.Private, IgnoreBehaviour.IgnoreAllMessagesAndCommandsWithoutTarget, "/cmd", false),
            (ChatType.Private, IgnoreBehaviour.IgnoreAllMessagesAndCommandsWithoutTarget, "/cmd@test_bot", true),
            (ChatType.Group, IgnoreBehaviour.IgnoreAllMessagesAndCommandsWithoutTarget, "plain text", false),
            (ChatType.Group, IgnoreBehaviour.IgnoreAllMessagesAndCommandsWithoutTarget, "/cmd", false),
            (ChatType.Group, IgnoreBehaviour.IgnoreAllMessagesAndCommandsWithoutTarget, "/cmd@test_bot", true),
        ];

        foreach (var row in rows) {
            yield return [row.ChatType, row.Behaviour, row.Text, row.Expected];
        }
    }

    [Theory]
    [MemberData(nameof(IgnoreBehaviourScenarios))]
    public async Task Dispatch_IgnoreBehaviour_AppliesPerChatKind(ChatType chatType, IgnoreBehaviour behaviour, string text, bool expectHandlerCalled) {
        var harness = chatType == ChatType.Group
            ? DispatcherTestHarness.Create([typeof(IgnoreBehaviourProbeController)], groupChatBehaviour: behaviour)
            : DispatcherTestHarness.Create([typeof(IgnoreBehaviourProbeController)], privateChatBehaviour: behaviour);
        IgnoreBehaviourProbeController.CallCount = 0;
        if (expectHandlerCalled) {
            EnqueueChatCreation(harness, 1);
        }

        await harness.Dispatcher.DispatchUpdateAsync(ChatMessageUpdate(1, chatType, text), harness.Provider, TestContext.Current.CancellationToken);

        Assert.Equal(expectHandlerCalled ? 1 : 0, IgnoreBehaviourProbeController.CallCount);
        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        Assert.Equal(expectHandlerCalled ? 1 : 0, await context.Users.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Dispatch_UserUpdateEveryMessage_PersistsSenderChatRow() {
        var harness = DispatcherTestHarness.Create([], userUpdate: UserUpdate.EveryMessage);
        EnqueueChatCreation(harness, 1);
        var update = new Update {
            Id = 1,
            Message = new Message {
                Id = 1,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                From = new User { Id = 99, FirstName = "Sender" },
                Text = "hi"
            }
        };

        await harness.Dispatcher.DispatchUpdateAsync(update, harness.Provider, TestContext.Current.CancellationToken);

        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        Assert.NotNull(await context.Users.FindAsync([99L], TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("hi", false)]
    [InlineData("/cmd", true)]
    public async Task Dispatch_UserUpdateBotCommand_PersistsSenderOnlyForCommands(string text, bool expectSenderPersisted) {
        var harness = DispatcherTestHarness.Create([], userUpdate: UserUpdate.BotCommand);
        EnqueueChatCreation(harness, 1);
        var update = new Update {
            Id = 1,
            Message = new Message {
                Id = 1,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                From = new User { Id = 99, FirstName = "Sender" },
                Text = text
            }
        };

        await harness.Dispatcher.DispatchUpdateAsync(update, harness.Provider, TestContext.Current.CancellationToken);

        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        var senderRow = await context.Users.FindAsync([99L], TestContext.Current.CancellationToken);
        Assert.Equal(expectSenderPersisted, senderRow is not null);
    }

    [Fact]
    public async Task Dispatch_UserUpdatePrivateMessage_NeverPersistsSender() {
        var harness = DispatcherTestHarness.Create([], userUpdate: UserUpdate.PrivateMessage);
        EnqueueChatCreation(harness, 1);
        var update = new Update {
            Id = 1,
            Message = new Message {
                Id = 1,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                From = new User { Id = 99, FirstName = "Sender" },
                Text = "hi"
            }
        };

        await harness.Dispatcher.DispatchUpdateAsync(update, harness.Provider, TestContext.Current.CancellationToken);

        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        Assert.Null(await context.Users.FindAsync([99L], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Dispatch_SenderIdEqualsChatId_DoesNotAddSecondChatRow() {
        var harness = DispatcherTestHarness.Create([], userUpdate: UserUpdate.EveryMessage);
        EnqueueChatCreation(harness, 1);

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken);

        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        Assert.Equal(1, await context.Users.CountAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Dispatch_NewChat_AppliesFirstMatchingDefaultUserRole(bool matchByUserId) {
        IReadOnlyList<UserRole> roles = matchByUserId
            ? [new UserRole(1, ChatRole.Administrator)]
            : [new UserRole("someuser", ChatRole.Moderator)];
        var harness = DispatcherTestHarness.Create([], defaultUserRole: roles);
        harness.HttpHandler.Enqueue("getChat", new ChatFullInfo { Id = 1, Type = ChatType.Private, Description = "desc" });
        var update = new Update {
            Id = 1,
            Message = new Message {
                Id = 1,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private, Username = matchByUserId ? null : "someuser" },
                From = new User { Id = 1, FirstName = "User" },
                Text = "hi"
            }
        };

        await harness.Dispatcher.DispatchUpdateAsync(update, harness.Provider, TestContext.Current.CancellationToken);

        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        var chat = await context.Users.FindAsync([1L], TestContext.Current.CancellationToken);
        Assert.Equal(matchByUserId ? ChatRole.Administrator : ChatRole.Moderator, chat!.Role);
    }

    [Fact]
    public async Task Dispatch_NewChat_NoMatchingDefaultUserRole_LeavesRoleAsUser() {
        var harness = DispatcherTestHarness.Create([], defaultUserRole: [new UserRole(999, ChatRole.Administrator)]);
        EnqueueChatCreation(harness, 1);

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken);

        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        var chat = await context.Users.FindAsync([1L], TestContext.Current.CancellationToken);
        Assert.Equal(ChatRole.User, chat!.Role);
    }

    [Fact]
    public async Task Dispatch_ExistingChat_UpdatesOnlyNonNullIncomingFields() {
        var harness = DispatcherTestHarness.Create([]);
        await using (var seedContext = TestTelegramContext.Create(harness.DatabaseName)) {
            seedContext.Users.Add(new TelegramChat(1) {
                Type = ChatType.Private,
                Username = "old_username",
                Title = "old_title",
                FirstName = "OldFirst",
                LastName = "OldLast"
            });
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var update = new Update {
            Id = 1,
            Message = new Message {
                Id = 1,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private, Username = "new_username" },
                From = new User { Id = 1, FirstName = "User" },
                Text = "hi"
            }
        };

        await harness.Dispatcher.DispatchUpdateAsync(update, harness.Provider, TestContext.Current.CancellationToken);

        Assert.Equal(0, harness.HttpHandler.CallCount("getChat"));
        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        var chat = await context.Users.FindAsync([1L], TestContext.Current.CancellationToken);
        Assert.Equal("new_username", chat!.Username);
        Assert.Equal("old_title", chat.Title);
        Assert.Equal("OldFirst", chat.FirstName);
        Assert.Equal("OldLast", chat.LastName);
    }

    [Fact]
    public async Task Dispatch_NewChat_CopiesChatFullInfoFields() {
        var harness = DispatcherTestHarness.Create([]);
        harness.HttpHandler.Enqueue("getChat", new ChatFullInfo {
            Id = 1, Type = ChatType.Private,
            Description = "a description",
            InviteLink = "https://t.me/joinchat/xyz",
            StickerSetName = "stickers",
            CanSetStickerSet = true
        });

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken);

        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        var chat = await context.Users.FindAsync([1L], TestContext.Current.CancellationToken);
        Assert.Equal("a description", chat!.Description);
        Assert.Equal("https://t.me/joinchat/xyz", chat.InviteLink);
        Assert.Equal("stickers", chat.StickerSetName);
        Assert.True(chat.CanSetStickerSet);
    }

    [Fact]
    public async Task Dispatch_VoidReturningHandler_IsInvoked() {
        var harness = DispatcherTestHarness.Create([typeof(VoidHandlerController)]);
        EnqueueChatCreation(harness, 1);
        VoidHandlerController.Called = false;

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken);

        Assert.True(VoidHandlerController.Called);
    }

    [Fact]
    public async Task Dispatch_ChatIsPersistedBeforeHandlerRuns() {
        var harness = DispatcherTestHarness.Create([typeof(PrePersistenceProbeController)]);
        EnqueueChatCreation(harness, 1);
        PrePersistenceProbeController.ChatExistedWhenHandlerRan = null;

        await harness.Dispatcher.DispatchUpdateAsync(TextMessageUpdate(1, "hi"), harness.Provider, TestContext.Current.CancellationToken);

        Assert.True(PrePersistenceProbeController.ChatExistedWhenHandlerRan);
    }

    [Theory]
    [InlineData("EditedMessage")]
    [InlineData("ChannelPost")]
    [InlineData("CallbackQueryMessage")]
    public async Task Dispatch_NonMessageUpdateShapes_LoadChatAndRunHandlers(string shape) {
        var harness = DispatcherTestHarness.Create([typeof(MessageCommandCapturingFallbackController)]);
        EnqueueChatCreation(harness, 1);
        MessageCommandCapturingFallbackController.Captured = null;
        var message = new Message {
            Id = 1, Date = DateTime.UtcNow,
            Chat = new Chat { Id = 1, Type = ChatType.Private },
            From = new User { Id = 1, FirstName = "User" },
            Text = "hi"
        };
        Update update = shape switch {
            "EditedMessage" => new Update { Id = 1, EditedMessage = message },
            "ChannelPost" => new Update { Id = 1, ChannelPost = message },
            "CallbackQueryMessage" => new Update { Id = 1, CallbackQuery = new CallbackQuery { Id = "1", From = new User { Id = 1, FirstName = "u" }, Message = message } },
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };

        await harness.Dispatcher.DispatchUpdateAsync(update, harness.Provider, TestContext.Current.CancellationToken);

        Assert.NotNull(MessageCommandCapturingFallbackController.Captured);
        Assert.True(MessageCommandCapturingFallbackController.Captured!.IsEmpty());
        Assert.False(MessageCommandCapturingFallbackController.Captured!.IsCommand());
        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        Assert.NotNull(await context.Users.FindAsync([1L], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Dispatch_UpdateWithoutMessage_DoesNotTouchDatabaseOrCallGetChat() {
        var harness = DispatcherTestHarness.Create([]);
        var update = new Update { Id = 1, Poll = new Poll { Id = "1", Question = "q", Options = [] } };

        await harness.Dispatcher.DispatchUpdateAsync(update, harness.Provider, TestContext.Current.CancellationToken);

        Assert.Equal(0, harness.HttpHandler.CallCount("getChat"));
        await using var context = TestTelegramContext.Create(harness.DatabaseName);
        Assert.Equal(0, await context.Users.CountAsync(TestContext.Current.CancellationToken));
    }
}
