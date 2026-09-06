using System.Text.Json;
using NSubstitute;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Advanced.Tests.Infrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Tests;

/// <summary>
/// Covers <see cref="NewsletterSubscriptionController{TContext}"/> and
/// <see cref="NewsletterPublishingController{TContext}"/> directly (bypassing the Dispatcher's filter pipeline,
/// which is exercised separately in DispatcherRoutingTests/FilterAndParsingTests): usage replies, subscribe state
/// replies, mid-flow newsletter deletion, and photo/audio publishing selection.
/// </summary>
public sealed class NewsletterControllerTests {
    private sealed record Fixture<TController>(
        TController Controller,
        TestTelegramContext Context,
        TestHttpMessageHandler Http,
        INewsletterService Service,
        TelegramChat Chat) where TController : ITelegramController<TestTelegramContext>;

    private static Fixture<TController> Create<TController>(Func<INewsletterService, TController> factory, long chatId = 1)
        where TController : ITelegramController<TestTelegramContext> {
        var service = Substitute.For<INewsletterService>();
        var controller = factory(service);
        var (client, http) = TestBotClientFactory.Create();
        var botData = Substitute.For<ITelegramBotData>();
        botData.Bot.Returns(client);
        var context = TestTelegramContext.Create();
        var chat = new TelegramChat(chatId) { Type = ChatType.Private };
        context.Users.Add(chat);
        context.SaveChanges();

        controller.BotData = botData;
        controller.TelegramContext = context;
        controller.TelegramChat = chat;
        controller.Update = new Update {
            Id = 1,
            Message = new Message {
                Id = 1,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = chatId, Type = ChatType.Private },
                From = new User { Id = chatId, FirstName = "User" }
            }
        };
        controller.MessageCommand = new MessageCommand();
        controller.CancellationToken = TestContext.Current.CancellationToken;

        return new Fixture<TController>(controller, context, http, service, chat);
    }

    private static MessageCommand CommandWithArgs(string command, params string[] args) =>
        new(new Message { Text = $"/{command}" + (args.Length > 0 ? " " + string.Join(' ', args) : "") });

    private static string LastSendMessageText(TestHttpMessageHandler http) {
        var request = Assert.Single(http.Requests, r => r.Method == "sendMessage");
        using var doc = JsonDocument.Parse(request.Body!);
        return doc.RootElement.GetProperty("text").GetString()!;
    }

    // ---- usage replies ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task SubscribeToNewsletter_WrongParameterCount_RepliesUsageWithoutCallingService(int argCount) {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("subscribe", [.. Enumerable.Range(0, argCount).Select(i => $"a{i}")]);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SubscribeToNewsletter();

        Assert.Equal("Usage:\n/subscribe <newsletter>", LastSendMessageText(fixture.Http));
        await fixture.Service.DidNotReceive().GetNewsletterByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await fixture.Service.DidNotReceive().SubscribeChatAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task UnsubscribeFromNewsletter_WrongParameterCount_RepliesUsageWithoutCallingService(int argCount) {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("unsubscribe", [.. Enumerable.Range(0, argCount).Select(i => $"a{i}")]);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.UnsubscribeFromNewsletter();

        Assert.Equal("Usage:\n/unsubscribe <newsletter>", LastSendMessageText(fixture.Http));
        await fixture.Service.DidNotReceive().GetNewsletterByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await fixture.Service.DidNotReceive().UnsubscribeChatAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task SendNewsletter_WrongParameterCount_RepliesUsageWithoutCallingService(int argCount) {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("send_newsletter", [.. Enumerable.Range(0, argCount).Select(i => $"a{i}")]);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendNewsletter();

        Assert.Equal("Usage:\n/send_newsletter <newsletter>", LastSendMessageText(fixture.Http));
        await fixture.Service.DidNotReceive().GetNewsletterByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ---- unsubscribe reply state -------------------------------------------------------------------------------

    [Fact]
    public async Task UnsubscribeFromNewsletter_Subscribed_RepliesSuccess() {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("unsubscribe", "news");
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        fixture.Service.UnsubscribeChatAsync("news", 1, Arg.Any<CancellationToken>()).Returns(true);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.UnsubscribeFromNewsletter();

        Assert.Equal("Successfully unsubscribed from the news newsletter", LastSendMessageText(fixture.Http));
    }

    [Fact]
    public async Task UnsubscribeFromNewsletter_NotSubscribed_RepliesAlreadyUnsubscribed() {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("unsubscribe", "news");
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        fixture.Service.UnsubscribeChatAsync("news", 1, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.UnsubscribeFromNewsletter();

        Assert.Equal("You are not subscribed to this newsletter", LastSendMessageText(fixture.Http));
    }

    // ---- newsletter deleted mid-flow --------------------------------------------------------------------------

    [Fact]
    public async Task SendNewsletterGetText_NewsletterDeletedBeforeContentArrives_RepliesNoticeClearsStateAndSaves() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Chat.State = "TBA_sendingNewsletter";
        fixture.Chat["newsletter"] = "news";
        fixture.Controller.Update = new Update {
            Id = 2,
            Message = new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }, Text = "the content" }
        };
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns((Newsletter?) null);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 3, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendNewsletterGetText();

        Assert.Contains("was deleted", LastSendMessageText(fixture.Http));
        Assert.Null(fixture.Chat.State);
        Assert.Null(fixture.Chat["newsletter"]);
        await fixture.Service.DidNotReceive().SendNewsletterAsync(
            Arg.Any<string>(), Arg.Any<Func<TelegramChat, CancellationToken, Task>>(), Arg.Any<CancellationToken>());

        var persisted = await TelegramChat.GetAsync(fixture.Context, 1, TestContext.Current.CancellationToken);
        Assert.NotNull(persisted);
        Assert.Null(persisted!.State);
    }

    // ---- photo / audio publishing -------------------------------------------------------------------------------

    [Fact]
    public async Task SendNewsletterGetText_PhotoMessage_SendsLastPhotoWithCaptionAndEntities() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Chat.State = "TBA_sendingNewsletter";
        fixture.Chat["newsletter"] = "news";
        var entities = new[] { new MessageEntity { Type = MessageEntityType.Bold, Offset = 0, Length = 3 } };
        fixture.Controller.Update = new Update {
            Id = 2,
            Message = new Message {
                Id = 2,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                Photo = [
                    new PhotoSize { FileId = "small", FileUniqueId = "u1", Width = 10, Height = 10 },
                    new PhotoSize { FileId = "large", FileUniqueId = "u2", Width = 100, Height = 100 }
                ],
                Caption = "look at this",
                CaptionEntities = entities
            }
        };
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        Func<TelegramChat, CancellationToken, Task>? capturedSend = null;
        fixture.Service.SendNewsletterAsync(
                "news",
                Arg.Do<Func<TelegramChat, CancellationToken, Task>>(f => capturedSend = f),
                Arg.Any<CancellationToken>())
            .Returns(new SendResult(0, new Dictionary<TelegramChat, Exception>()));
        fixture.Http.Enqueue("sendMessage", new Message { Id = 3, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });
        fixture.Http.Enqueue("sendMessage", new Message { Id = 4, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendNewsletterGetText();

        Assert.NotNull(capturedSend);
        fixture.Http.Enqueue("sendPhoto", new Message { Id = 5, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });
        var recipient = new TelegramChat(2) { Type = ChatType.Private };
        await capturedSend!(recipient, TestContext.Current.CancellationToken);

        var photoRequest = Assert.Single(fixture.Http.Requests, r => r.Method == "sendPhoto");
        using var doc = JsonDocument.Parse(photoRequest.Body!);
        var root = doc.RootElement;
        Assert.Equal("large", root.GetProperty("photo").GetString());
        Assert.Equal("look at this", root.GetProperty("caption").GetString());
        Assert.Equal("Html", root.GetProperty("parse_mode").GetString());
        Assert.Equal(1, root.GetProperty("caption_entities").GetArrayLength());
        Assert.Equal(0, root.GetProperty("caption_entities")[0].GetProperty("offset").GetInt32());
        Assert.Equal(3, root.GetProperty("caption_entities")[0].GetProperty("length").GetInt32());
    }

    [Fact]
    public async Task SendNewsletterGetText_AudioMessage_SendsAudioWithCaptionAndEntities() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Chat.State = "TBA_sendingNewsletter";
        fixture.Chat["newsletter"] = "news";
        var entities = new[] { new MessageEntity { Type = MessageEntityType.Italic, Offset = 1, Length = 4 } };
        fixture.Controller.Update = new Update {
            Id = 2,
            Message = new Message {
                Id = 2,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                Audio = new Audio { FileId = "audio-file", FileUniqueId = "u1", Duration = 30 },
                Caption = "listen to this",
                CaptionEntities = entities
            }
        };
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        Func<TelegramChat, CancellationToken, Task>? capturedSend = null;
        fixture.Service.SendNewsletterAsync(
                "news",
                Arg.Do<Func<TelegramChat, CancellationToken, Task>>(f => capturedSend = f),
                Arg.Any<CancellationToken>())
            .Returns(new SendResult(0, new Dictionary<TelegramChat, Exception>()));
        fixture.Http.Enqueue("sendMessage", new Message { Id = 3, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });
        fixture.Http.Enqueue("sendMessage", new Message { Id = 4, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendNewsletterGetText();

        Assert.NotNull(capturedSend);
        fixture.Http.Enqueue("sendAudio", new Message { Id = 5, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });
        var recipient = new TelegramChat(2) { Type = ChatType.Private };
        await capturedSend!(recipient, TestContext.Current.CancellationToken);

        var audioRequest = Assert.Single(fixture.Http.Requests, r => r.Method == "sendAudio");
        using var doc = JsonDocument.Parse(audioRequest.Body!);
        var root = doc.RootElement;
        Assert.Equal("audio-file", root.GetProperty("audio").GetString());
        Assert.Equal("listen to this", root.GetProperty("caption").GetString());
        Assert.Equal(1, root.GetProperty("caption_entities").GetArrayLength());
        Assert.Equal(1, root.GetProperty("caption_entities")[0].GetProperty("offset").GetInt32());
        Assert.Equal(4, root.GetProperty("caption_entities")[0].GetProperty("length").GetInt32());
    }
}
