using NSubstitute;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Core.Tools;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Tests;

/// <summary>
/// Covers every dispatcher filter's true/false boundary, <see cref="MessageCommand"/>/<see cref="InlineDataWrapper"/>
/// parsing, <see cref="Utils.ObfuscateToken"/>, and every <see cref="UpdateExtensions.GetMessage"/> shape.
/// </summary>
public sealed class FilterAndParsingTests {
    private static readonly MessageCommand EmptyCommand = new();
    private static readonly ITelegramBotData NoBotData = Substitute.For<ITelegramBotData>();

    private static Update MessageUpdate(Chat chat, string? text = null) => new() {
        Id = 1,
        Message = new Message { Id = 1, Date = DateTime.UtcNow, Chat = chat, Text = text }
    };

    [Fact]
    public void DefaultChatStateFilter_IsValid_NullChat_ReturnsTrue() {
        var filter = new DefaultChatStateFilter();
        Assert.True(filter.IsValid(MessageUpdate(new Chat { Id = 1, Type = ChatType.Private }), null, EmptyCommand, NoBotData));
    }

    [Fact]
    public void DefaultChatStateFilter_IsValid_ChatWithNullState_ReturnsTrue() {
        var filter = new DefaultChatStateFilter();
        var chat = new TelegramChat(1) { State = null };
        Assert.True(filter.IsValid(MessageUpdate(new Chat { Id = 1, Type = ChatType.Private }), chat, EmptyCommand, NoBotData));
    }

    [Fact]
    public void DefaultChatStateFilter_IsValid_ChatWithNonNullState_ReturnsFalse() {
        var filter = new DefaultChatStateFilter();
        var chat = new TelegramChat(1) { State = "some-state" };
        Assert.False(filter.IsValid(MessageUpdate(new Chat { Id = 1, Type = ChatType.Private }), chat, EmptyCommand, NoBotData));
    }

    [Fact]
    public void CallbackCommandFilter_IsValid_NullData_ReturnsFalseWithoutThrowing() {
        var filter = new CallbackCommandFilter("cmd");
        var update = new Update { Id = 1, CallbackQuery = new CallbackQuery { Id = "1", From = new User { Id = 1, FirstName = "u" }, Data = null } };

        var exception = Record.Exception(() => filter.IsValid(update, null, EmptyCommand, NoBotData));

        Assert.Null(exception);
        Assert.False(filter.IsValid(update, null, EmptyCommand, NoBotData));
    }

    [Theory]
    [InlineData("cmd&x=1", "cmd", true)]
    [InlineData("other&x=1", "cmd", false)]
    public void CallbackCommandFilter_IsValid_MatchingCommand_ReturnsTrue(string callbackData, string configuredCommand, bool expected) {
        var filter = new CallbackCommandFilter(configuredCommand);
        var update = new Update {
            Id = 1,
            CallbackQuery = new CallbackQuery { Id = "1", From = new User { Id = 1, FirstName = "u" }, Data = callbackData }
        };

        Assert.Equal(expected, filter.IsValid(update, null, EmptyCommand, NoBotData));
    }

    [Theory]
    [InlineData("start", true)]
    [InlineData("stop", false)]
    public void CommandFilter_IsValid_MatchesConfiguredCommands(string actualCommand, bool expected) {
        var filter = new CommandFilter("start", "help");
        var command = new MessageCommand(new Message { Text = $"/{actualCommand}" });

        Assert.Equal(expected, filter.IsValid(MessageUpdate(new Chat { Id = 1, Type = ChatType.Private }), null, command, NoBotData));
    }

    [Theory]
    [InlineData(ChatType.Private, true)]
    [InlineData(ChatType.Group, false)]
    public void ChatTypeFilter_IsValid_MatchesConfiguredTypes(ChatType actual, bool expected) {
        var filter = new ChatTypeFilter(ChatType.Private, ChatType.Sender);
        var update = MessageUpdate(new Chat { Id = 1, Type = actual });

        Assert.Equal(expected, filter.IsValid(update, null, EmptyCommand, NoBotData));
    }

    [Theory]
    [InlineData(ChatRole.Administrator, true)]
    [InlineData(ChatRole.User, false)]
    public void ChatRoleFilter_IsValid_MatchesConfiguredRoles(ChatRole actual, bool expected) {
        var filter = new ChatRoleFilter(ChatRole.Administrator, ChatRole.Moderator);
        var chat = new TelegramChat(1) { Role = actual };

        Assert.Equal(expected, filter.IsValid(MessageUpdate(new Chat { Id = 1, Type = ChatType.Private }), chat, EmptyCommand, NoBotData));
    }

    [Fact]
    public void ChatRoleFilter_IsValid_NullChat_ReturnsFalse() {
        var filter = new ChatRoleFilter(ChatRole.Administrator);
        Assert.False(filter.IsValid(MessageUpdate(new Chat { Id = 1, Type = ChatType.Private }), null, EmptyCommand, NoBotData));
    }

    [Theory]
    [InlineData("main", true)]
    [InlineData("other", false)]
    public void EndpointFilter_IsValid_MatchesConfiguredEndpoints(string actualEndpoint, bool expected) {
        var filter = new EndpointFilter("main", "secondary");
        var botData = Substitute.For<ITelegramBotData>();
        botData.Endpoint.Returns(actualEndpoint);

        Assert.Equal(expected, filter.IsValid(MessageUpdate(new Chat { Id = 1, Type = ChatType.Private }), null, EmptyCommand, botData));
    }

    [Theory]
    [InlineData(MessageType.Text, true)]
    [InlineData(MessageType.Photo, false)]
    public void MessageTypeFilter_IsValid_MatchesConfiguredTypes(MessageType actual, bool expected) {
        var filter = new MessageTypeFilter(MessageType.Text, MessageType.Sticker);
        var message = actual == MessageType.Text
            ? new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }, Text = "hi" }
            : new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }, Photo = [new PhotoSize { FileId = "f", FileUniqueId = "u", Width = 1, Height = 1 }] };
        var update = new Update { Id = 1, Message = message };

        Assert.Equal(expected, filter.IsValid(update, null, EmptyCommand, NoBotData));
    }

    [Theory]
    [InlineData(UpdateType.Message, true)]
    [InlineData(UpdateType.CallbackQuery, false)]
    public void UpdateTypeFilter_IsValid_MatchesConfiguredTypes(UpdateType actual, bool expected) {
        var filter = new UpdateTypeFilter(UpdateType.Message, UpdateType.EditedMessage);
        var update = actual == UpdateType.Message
            ? MessageUpdate(new Chat { Id = 1, Type = ChatType.Private })
            : new Update { Id = 1, CallbackQuery = new CallbackQuery { Id = "1", From = new User { Id = 1, FirstName = "u" } } };

        Assert.Equal(expected, filter.IsValid(update, null, EmptyCommand, NoBotData));
    }

    [Theory]
    [InlineData(2, 2, true)]
    [InlineData(2, 3, false)]
    public void ParametersFilter_IsValid_MatchesExactCount(int configured, int actualCount, bool expected) {
        var filter = new ParametersFilter(configured);
        var command = new MessageCommand(new Message { Text = "/cmd " + string.Join(' ', Enumerable.Range(0, actualCount).Select(i => $"p{i}")) });

        Assert.Equal(expected, filter.IsValid(MessageUpdate(new Chat { Id = 1, Type = ChatType.Private }), null, command, NoBotData));
    }

    [Fact]
    public void NoCommandFilter_IsValid_TrueWhenNotACommand() {
        var filter = new NoCommandFilter();
        var plainText = new MessageCommand(new Message { Text = "just chatting" });
        var command = new MessageCommand(new Message { Text = "/start" });

        Assert.True(filter.IsValid(MessageUpdate(new Chat { Id = 1, Type = ChatType.Private }), null, plainText, NoBotData));
        Assert.False(filter.IsValid(MessageUpdate(new Chat { Id = 1, Type = ChatType.Private }), null, command, NoBotData));
    }

    [Fact]
    public void MessageCommand_ParsesCommandTargetAndParameters() {
        var command = new MessageCommand(new Message { Text = "/broadcast@my_bot hello world" });

        Assert.Equal("broadcast", command.Command);
        Assert.Equal("my_bot", command.Target);
        Assert.Equal(["hello", "world"], command.Parameters);
        Assert.True(command.IsCommand());
    }

    [Fact]
    public void MessageCommand_NoTarget_TargetIsNull() {
        var command = new MessageCommand(new Message { Text = "/start" });

        Assert.Null(command.Target);
        Assert.Empty(command.Parameters);
    }

    [Fact]
    public void MessageCommand_NullText_LeavesAllFieldsNullAndIsEmptyTrue() {
        var command = new MessageCommand(new Message { Text = null });

        Assert.True(command.IsEmpty());
        Assert.False(command.IsCommand());
        Assert.Null(command.Text);
        Assert.Null(command.Command);
        Assert.Null(command.Target);
        Assert.Empty(command.Parameters);
    }

    [Fact]
    public void MessageCommand_PlainNonCommandText_CommandNullButTextPreserved() {
        var command = new MessageCommand(new Message { Text = "just chatting" });

        Assert.False(command.IsCommand());
        Assert.False(command.IsEmpty());
        Assert.Equal("just chatting", command.Text);
        Assert.Null(command.Command);
        Assert.Null(command.Target);
        Assert.Empty(command.Parameters);
    }

    [Fact]
    public void MessageCommand_MultipleConsecutiveSpaces_ParametersCollapseEmptyEntries() {
        var command = new MessageCommand(new Message { Text = "/cmd  a   b" });

        Assert.Equal("cmd", command.Command);
        Assert.Equal(["a", "b"], command.Parameters);
    }

    [Fact]
    public void MessageCommand_TargetOnlyNoTrailingText_ParametersEmptyMessageEmpty() {
        var command = new MessageCommand(new Message { Text = "/broadcast@my_bot" });

        Assert.Equal("broadcast", command.Command);
        Assert.Equal("my_bot", command.Target);
        Assert.Empty(command.Parameters);
        Assert.Equal("", command.Message);
    }

    [Fact]
    public void InlineDataWrapper_ToStringThenParseInlineData_RoundTripsCommandAndMultiKeyData() {
        var wrapper = new InlineDataWrapper("cmd", new Dictionary<string, string> { ["x"] = "1", ["y"] = "two words" });

        var serialized = wrapper.ToString();
        var parsed = InlineDataWrapper.ParseInlineData(serialized);

        Assert.Equal("cmd", parsed.Command);
        Assert.Equal("1", parsed.Data["x"]);
        Assert.Equal("two words", parsed.Data["y"]);
    }


    [Fact]
    public void InlineDataWrapper_ParseInlineData_MalformedInput_ReturnsEmptyNonThrowingWrapper() {
        InlineDataWrapper? parsed = null;
        var exception = Record.Exception(() => parsed = InlineDataWrapper.ParseInlineData("no-ampersand-here"));

        Assert.Null(exception);
        Assert.NotNull(parsed);
        Assert.Null(parsed!.Command);
        Assert.Empty(parsed.Data);
    }

    [Fact]
    public void InlineDataWrapper_ToString_ExceedsMaximumSize_ThrowsMaximumSizeExceededException() {
        var wrapper = new InlineDataWrapper("cmd", new Dictionary<string, string> { ["payload"] = new string('x', 100) });

        Assert.Throws<MaximumSizeExceededException>(() => wrapper.ToString());
    }

    [Fact]
    public void Utils_ObfuscateToken_RedactsFullTokenSuffixNotJustFixedLength() {
        // The suffix below is 47 chars, well past a fixed 35-char truncation - proves the whole \S+ run is
        // replaced, not just a fixed-length prefix of it.
        const string secretSuffix = "AbCdEfGhIjKlMnOpQrStUvWxYz1234567890ABCDEFGHIJ";
        var input = $"token=123456789:{secretSuffix} end";

        var result = Utils.ObfuscateToken(input);

        Assert.Equal("token=00000000:XXXXXXXXXX end", result);
        Assert.DoesNotContain(secretSuffix, result);
    }

    public static IEnumerable<object[]> SupportedMessageShapes() {
        var message = new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }, Text = "hi" };
        yield return [new Update { Id = 1, Message = message }, message];
        yield return [new Update { Id = 1, EditedMessage = message }, message];
        yield return [new Update { Id = 1, ChannelPost = message }, message];
        yield return [new Update { Id = 1, EditedChannelPost = message }, message];
        yield return [new Update { Id = 1, BusinessMessage = message }, message];
        yield return [new Update { Id = 1, EditedBusinessMessage = message }, message];
        yield return [
            new Update { Id = 1, CallbackQuery = new CallbackQuery { Id = "1", From = new User { Id = 1, FirstName = "u" }, Message = message } },
            message
        ];
    }

    [Theory]
    [MemberData(nameof(SupportedMessageShapes))]
    public void GetMessage_ReturnsMessageForEverySupportedUpdateShape(Update update, Message expected) {
        Assert.Same(expected, update.GetMessage());
    }

    [Fact]
    public void GetMessage_UnsupportedUpdateShape_ReturnsNull() {
        var update = new Update { Id = 1, Poll = new Poll { Id = "1", Question = "q", Options = [] } };

        Assert.Null(update.GetMessage());
    }
}
