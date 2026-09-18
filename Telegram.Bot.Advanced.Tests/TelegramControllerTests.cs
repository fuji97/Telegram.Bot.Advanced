using System.Text.Json;
using NSubstitute;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Tests.Infrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Advanced.Tests;

/// <summary>
/// Covers the protected reply helpers on <see cref="TelegramController{TContext}"/>
/// (<c>ReplyTextMessageAsync</c>, <c>ReplyPhotoAsync</c>, <c>ReplyStickerAsync</c>) via a thin exposing subclass.
/// </summary>
public sealed class TelegramControllerTests {
    private sealed class ExposedController : TelegramController<TestTelegramContext> {
        public Task<Message> CallReplyText(string text, ParseMode parseMode = default,
            ReplyParameters? replyParameters = null, CancellationToken cancellationToken = default) =>
            ReplyTextMessageAsync(text, parseMode, replyParameters: replyParameters, cancellationToken: cancellationToken);

        public Task<Message> CallReplyPhoto(InputFile photo, string? caption,
            IEnumerable<MessageEntity>? captionEntities, CancellationToken cancellationToken = default) =>
            ReplyPhotoAsync(photo, caption, captionEntities: captionEntities, cancellationToken: cancellationToken);

        public Task<Message> CallReplySticker(InputFile sticker, CancellationToken cancellationToken = default) =>
            ReplyStickerAsync(sticker, cancellationToken: cancellationToken);
    }

    [Fact]
    public async Task ReplyTextMessageAsync_SendsToCurrentChatWithParseModeAndReplyParameters() {
        var (client, http) = TestBotClientFactory.Create();
        var controller = new ExposedController {
            BotData = Substitute.For<ITelegramBotData>(),
            TelegramChat = new TelegramChat(42) { Type = ChatType.Private }
        };
        controller.BotData.Bot.Returns(client);
        http.Enqueue("sendMessage", new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 42, Type = ChatType.Private } });

        await controller.CallReplyText("hello", ParseMode.Html, new ReplyParameters { MessageId = 7 }, TestContext.Current.CancellationToken);

        var request = Assert.Single(http.Requests, r => r.Method == "sendMessage");
        using var doc = JsonDocument.Parse(request.Body!);
        var root = doc.RootElement;
        Assert.Equal(42, root.GetProperty("chat_id").GetInt64());
        Assert.Equal("hello", root.GetProperty("text").GetString());
        Assert.Equal("Html", root.GetProperty("parse_mode").GetString());
        Assert.Equal(7, root.GetProperty("reply_parameters").GetProperty("message_id").GetInt32());
    }

    [Fact]
    public async Task ReplyPhotoAsync_SendsCaptionAndCaptionEntities() {
        var (client, http) = TestBotClientFactory.Create();
        var controller = new ExposedController {
            BotData = Substitute.For<ITelegramBotData>(),
            TelegramChat = new TelegramChat(42) { Type = ChatType.Private }
        };
        controller.BotData.Bot.Returns(client);
        http.Enqueue("sendPhoto", new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 42, Type = ChatType.Private } });
        var entities = new[] { new MessageEntity { Type = MessageEntityType.Bold, Offset = 0, Length = 3 } };

        await controller.CallReplyPhoto(InputFile.FromFileId("photo123"), "a caption", entities, TestContext.Current.CancellationToken);

        var request = Assert.Single(http.Requests, r => r.Method == "sendPhoto");
        using var doc = JsonDocument.Parse(request.Body!);
        var root = doc.RootElement;
        Assert.Equal("a caption", root.GetProperty("caption").GetString());
        Assert.Equal(1, root.GetProperty("caption_entities").GetArrayLength());
    }

    [Fact]
    public async Task ReplyStickerAsync_SendsStickerFileId() {
        var (client, http) = TestBotClientFactory.Create();
        var controller = new ExposedController {
            BotData = Substitute.For<ITelegramBotData>(),
            TelegramChat = new TelegramChat(42) { Type = ChatType.Private }
        };
        controller.BotData.Bot.Returns(client);
        http.Enqueue("sendSticker", new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 42, Type = ChatType.Private } });

        await controller.CallReplySticker(InputFile.FromFileId("sticker123"), TestContext.Current.CancellationToken);

        var request = Assert.Single(http.Requests, r => r.Method == "sendSticker");
        using var doc = JsonDocument.Parse(request.Body!);
        Assert.Equal("sticker123", doc.RootElement.GetProperty("sticker").GetString());
    }

    [Fact]
    public async Task ReplyTextMessageAsync_CancelledTokenArgument_ThrowsOperationCanceledExceptionAndSendsNothing() {
        var (client, http) = TestBotClientFactory.Create();
        var controller = new ExposedController {
            BotData = Substitute.For<ITelegramBotData>(),
            TelegramChat = new TelegramChat(42) { Type = ChatType.Private }
        };
        controller.BotData.Bot.Returns(client);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.CallReplyText("hello", cancellationToken: cts.Token));

        Assert.Empty(http.Requests);
    }
}
