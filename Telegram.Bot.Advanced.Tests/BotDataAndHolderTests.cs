using NSubstitute;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Tests.Infrastructure;

namespace Telegram.Bot.Advanced.Tests;

/// <summary>
/// Covers <see cref="TelegramBotData"/>, <see cref="TelegramBotDataOptions"/>, and <see cref="TelegramHolder"/>:
/// endpoint/base-path validation, default values, dispatcher-vs-builder precedence, and holder lookup semantics.
/// </summary>
public sealed class BotDataAndHolderTests {
    private sealed class ProbeController : TelegramController<TestTelegramContext> {
        [NoMethodFilter]
        public Task Handle() => Task.CompletedTask;
    }

    [Fact]
    public void Constructor_NullOptionsAction_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() => new TelegramBotData(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("a{b")]
    [InlineData("a}b")]
    [InlineData("a..b")]
    [InlineData("123456789:AAHtestToken")]
    public void Constructor_InvalidEndpoint_ThrowsArgumentException(string? endpoint) {
        var (client, _) = TestBotClientFactory.Create();

        var exception = Assert.Throws<ArgumentException>(() => new TelegramBotData(o => {
            o.Endpoint = endpoint;
            o.Bot = client;
            o.Dispatcher = Substitute.For<IDispatcher>();
        }));

        Assert.Equal("endpoint", exception.ParamName);
    }

    [Fact]
    public void Constructor_ValidEndpointWithoutBot_ThrowsArgumentException() {
        var exception = Assert.Throws<ArgumentException>(() => new TelegramBotData(o => {
            o.Endpoint = "mybot";
            o.Dispatcher = Substitute.For<IDispatcher>();
        }));

        Assert.Contains("CreateTelegramBotClient", exception.Message);
    }

    [Theory]
    [InlineData("", "/")]
    [InlineData("///", "/")]
    [InlineData("telegram", "/telegram/")]
    [InlineData("/proxy/telegram/", "/proxy/telegram/")]
    [InlineData("proxy/telegram", "/proxy/telegram/")]
    public void Constructor_BasePath_IsNormalized(string basePath, string expected) {
        var (client, _) = TestBotClientFactory.Create();

        var botData = new TelegramBotData(o => {
            o.Endpoint = "mybot";
            o.Bot = client;
            o.BasePath = basePath;
            o.Dispatcher = Substitute.For<IDispatcher>();
        });

        Assert.Equal(expected, botData.BasePath);
    }

    [Fact]
    public void Constructor_DispatcherBuilderSupplied_TakesPrecedenceOverDispatcher() {
        var (client, _) = TestBotClientFactory.Create();
        var substituteDispatcher = Substitute.For<IDispatcher>();
        substituteDispatcher.GetControllersType().Returns((IList<Type>) []);
        var builder = new DispatcherBuilder<TestTelegramContext>().AddControllers(typeof(ProbeController));

        var botData = new TelegramBotData(o => {
            o.Endpoint = "mybot";
            o.Bot = client;
            o.Dispatcher = substituteDispatcher;
            o.DispatcherBuilder = builder;
        });

        Assert.Equal([typeof(ProbeController)], botData.Dispatcher.GetControllersType());
        Assert.NotSame(substituteDispatcher, botData.Dispatcher);
    }

    [Fact]
    public void Constructor_NeitherDispatcherNorBuilder_ThrowsInvalidOperationException() {
        var (client, _) = TestBotClientFactory.Create();

        Assert.Throws<InvalidOperationException>(() => new TelegramBotData(o => {
            o.Endpoint = "mybot";
            o.Bot = client;
        }));
    }

    [Fact]
    public void Constructor_DefaultUserRoleList_IsCopied() {
        var (client, _) = TestBotClientFactory.Create();
        List<UserRole> roles = [new UserRole(1, ChatRole.Administrator)];

        var botData = new TelegramBotData(o => {
            o.Endpoint = "mybot";
            o.Bot = client;
            o.Dispatcher = Substitute.For<IDispatcher>();
            o.DefaultUserRole = roles;
        });
        roles.Add(new UserRole(2, ChatRole.Moderator));

        Assert.Single(botData.DefaultUserRole);
    }

    [Fact]
    public void Constructor_Defaults_AreBasePathTelegramPrivateMessageAndGroupIgnoreNonCommand() {
        var (client, _) = TestBotClientFactory.Create();

        var botData = new TelegramBotData(o => {
            o.Endpoint = "mybot";
            o.Bot = client;
            o.Dispatcher = Substitute.For<IDispatcher>();
        });

        Assert.Equal("/telegram/", botData.BasePath);
        Assert.Equal(UserUpdate.PrivateMessage, botData.UserUpdate);
        Assert.Equal(IgnoreBehaviour.IgnoreNonCommandMessages, botData.GroupChatBehaviour);
        Assert.Equal(IgnoreBehaviour.IgnoreNothing, botData.PrivateChatBehaviour);
    }

    [Fact]
    public void Constructor_MakesNoBotApiCall() {
        var (client, http) = TestBotClientFactory.Create();

        _ = new TelegramBotData(o => {
            o.Endpoint = "mybot";
            o.Bot = client;
            o.Dispatcher = Substitute.For<IDispatcher>();
        });

        Assert.Empty(http.Requests);
    }

    [Fact]
    public void CreateTelegramBotClient_NullToken_ThrowsArgumentNullException() {
        var options = new TelegramBotDataOptions();

        Assert.Throws<ArgumentNullException>(() => options.CreateTelegramBotClient(null!));
    }

    [Fact]
    public void CreateTelegramBotClient_EmptyToken_ThrowsArgumentException() {
        var options = new TelegramBotDataOptions();

        Assert.Throws<ArgumentException>(() => options.CreateTelegramBotClient(""));
    }

    [Fact]
    public void CreateTelegramBotClient_ValidToken_SetsBot() {
        var options = new TelegramBotDataOptions();

        options.CreateTelegramBotClient("123456789:AAHtestToken");

        Assert.NotNull(options.Bot);
    }

    [Fact]
    public void TelegramHolder_NullBots_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() => new TelegramHolder(null!));
    }

    [Fact]
    public void TelegramHolder_DuplicateEndpoint_ThrowsArgumentException() {
        var first = Substitute.For<ITelegramBotData>();
        first.Endpoint.Returns("dup");
        var second = Substitute.For<ITelegramBotData>();
        second.Endpoint.Returns("dup");

        Assert.Throws<ArgumentException>(() => new TelegramHolder([first, second]));
    }

    [Fact]
    public void TelegramHolder_TryGet_IsCaseSensitiveOrdinal() {
        var bot = Substitute.For<ITelegramBotData>();
        bot.Endpoint.Returns("MyBot");
        var holder = new TelegramHolder([bot]);

        Assert.False(holder.TryGet("mybot", out _));
        Assert.True(holder.TryGet("MyBot", out var found));
        Assert.Same(bot, found);
    }

    [Fact]
    public void TelegramHolder_Enumerates_AllRegisteredBots() {
        var first = Substitute.For<ITelegramBotData>();
        first.Endpoint.Returns("first");
        var second = Substitute.For<ITelegramBotData>();
        second.Endpoint.Returns("second");
        var holder = new TelegramHolder([first, second]);

        Assert.Equal([first, second], holder.ToList());
    }
}
