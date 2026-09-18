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

    // ---- subscribe / unsubscribe replies ----------------------------------------------------------------------

    [Fact]
    public async Task SubscribeToNewsletter_MissingNewsletter_RepliesDoesNotExist() {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("subscribe", "news");
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns((Newsletter?) null);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SubscribeToNewsletter();

        Assert.Equal("The newsletter news doesn't exist.", LastSendMessageText(fixture.Http));
        await fixture.Service.DidNotReceive().SubscribeChatAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubscribeToNewsletter_Success_RepliesSubscribed() {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("subscribe", "news");
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        fixture.Service.SubscribeChatAsync("news", 1, Arg.Any<CancellationToken>()).Returns(true);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SubscribeToNewsletter();

        Assert.Equal("Successfully subscribed to the news newsletter", LastSendMessageText(fixture.Http));
    }

    [Fact]
    public async Task SubscribeToNewsletter_AlreadySubscribed_RepliesProbablyAlreadySubscribed() {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("subscribe", "news");
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        fixture.Service.SubscribeChatAsync("news", 1, Arg.Any<CancellationToken>()).Returns(false);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SubscribeToNewsletter();

        Assert.Equal("Can't subscribe to newsletter, probably you are already subscribed", LastSendMessageText(fixture.Http));
    }

    [Fact]
    public async Task UnsubscribeFromNewsletter_MissingNewsletter_RepliesDoesNotExist() {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("unsubscribe", "news");
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns((Newsletter?) null);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.UnsubscribeFromNewsletter();

        Assert.Equal("The newsletter news doesn't exist.", LastSendMessageText(fixture.Http));
        await fixture.Service.DidNotReceive().UnsubscribeChatAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    // ---- group admin gating -------------------------------------------------------------------------------------

    [Fact]
    public async Task SubscribeToNewsletter_InGroupWithoutAdminRights_SendsNothingAndSkipsService() {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Chat.Type = ChatType.Group;
        fixture.Controller.MessageCommand = CommandWithArgs("subscribe", "news");
        fixture.Http.Enqueue("getChatAdministrators", new ChatMember[] {
            new ChatMemberMember { User = new User { Id = 999, FirstName = "Other" } }
        });

        await fixture.Controller.SubscribeToNewsletter();

        Assert.Equal(0, fixture.Http.CallCount("sendMessage"));
        await fixture.Service.DidNotReceive().GetNewsletterByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubscribeToNewsletter_InGroupAsAdministrator_Proceeds() {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Chat.Type = ChatType.Group;
        fixture.Controller.MessageCommand = CommandWithArgs("subscribe", "news");
        fixture.Http.Enqueue("getChatAdministrators", new ChatMember[] {
            new ChatMemberMember { User = new User { Id = 1, FirstName = "User" } }
        });
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        fixture.Service.SubscribeChatAsync("news", 1, Arg.Any<CancellationToken>()).Returns(true);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Group } });

        await fixture.Controller.SubscribeToNewsletter();

        Assert.Equal("Successfully subscribed to the news newsletter", LastSendMessageText(fixture.Http));
    }

    [Fact]
    public async Task SubscribeToNewsletter_InGroupWithoutSender_SendsNothing() {
        var fixture = Create(s => new NewsletterSubscriptionController<TestTelegramContext>(s));
        fixture.Chat.Type = ChatType.Group;
        fixture.Controller.MessageCommand = CommandWithArgs("subscribe", "news");
        fixture.Controller.Update.Message!.From = null;

        await fixture.Controller.SubscribeToNewsletter();

        Assert.Equal(0, fixture.Http.CallCount("sendMessage"));
        Assert.Equal(0, fixture.Http.CallCount("getChatAdministrators"));
    }

    // ---- send newsletter state transitions -----------------------------------------------------------------------

    [Fact]
    public async Task SendNewsletter_ExistingNewsletter_SetsStateAndDataThenPromptsForHtml() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("send_newsletter", "news");
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendNewsletter();

        Assert.Equal("TBA_sendingNewsletter", fixture.Chat.State);
        Assert.Equal("news", fixture.Chat["newsletter"]);
        Assert.Equal("Ok, now send me the text formatted as HTML", LastSendMessageText(fixture.Http));

        var persisted = await TelegramChat.GetAsync(fixture.Context, 1, TestContext.Current.CancellationToken);
        Assert.Equal("TBA_sendingNewsletter", persisted!.State);
        Assert.Equal("news", persisted["newsletter"]);
    }

    [Fact]
    public async Task SendNewsletter_MissingNewsletter_RepliesWithCreateHint() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("send_newsletter", "news");
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns((Newsletter?) null);
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendNewsletter();

        Assert.Equal(
            "The newsletter news doesn't exist.\nUse /create_newsletter news - to create it",
            LastSendMessageText(fixture.Http));
        Assert.Null(fixture.Chat.State);
    }

    [Fact]
    public async Task SendGlobalNewsletter_WithParameters_RepliesUsage() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("send_global_newsletter", "extra");
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendGlobalNewsletter();

        Assert.Equal("Usage:\n/send_global_newsletter", LastSendMessageText(fixture.Http));
        Assert.Null(fixture.Chat.State);
    }

    [Fact]
    public async Task SendGlobalNewsletter_NoParameters_SetsStateWithNullNewsletterKey() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Controller.MessageCommand = CommandWithArgs("send_global_newsletter");
        fixture.Http.Enqueue("sendMessage", new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendGlobalNewsletter();

        Assert.Equal("TBA_sendingNewsletter", fixture.Chat.State);
        Assert.Null(fixture.Chat["newsletter"]);
        Assert.Equal("Ok, now send me the text formatted as HTML", LastSendMessageText(fixture.Http));
    }

    // ---- text / sticker publishing ---------------------------------------------------------------------------------

    [Fact]
    public async Task SendNewsletterGetText_TextMessage_SendsHtmlTextToEverySubscriber() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Chat.State = "TBA_sendingNewsletter";
        fixture.Chat["newsletter"] = "news";
        fixture.Controller.Update = new Update {
            Id = 2,
            Message = new Message {
                Id = 2,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                Text = "<b>hello</b>"
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
        fixture.Http.Enqueue("sendMessage", new Message { Id = 5, Date = DateTime.UtcNow, Chat = new Chat { Id = 2, Type = ChatType.Private } });
        fixture.Http.Enqueue("sendMessage", new Message { Id = 6, Date = DateTime.UtcNow, Chat = new Chat { Id = 3, Type = ChatType.Private } });
        await capturedSend!(new TelegramChat(2) { Type = ChatType.Private }, TestContext.Current.CancellationToken);
        await capturedSend!(new TelegramChat(3) { Type = ChatType.Private }, TestContext.Current.CancellationToken);

        var deliveryRequests = fixture.Http.Requests.Where(r => r.Method == "sendMessage").Skip(2).ToList();
        Assert.Equal(2, deliveryRequests.Count);
        foreach (var request in deliveryRequests) {
            using var doc = JsonDocument.Parse(request.Body!);
            Assert.Equal("<b>hello</b>", doc.RootElement.GetProperty("text").GetString());
            Assert.Equal("Html", doc.RootElement.GetProperty("parse_mode").GetString());
        }
    }

    [Fact]
    public async Task SendNewsletterGetText_StickerMessage_SendsStickerFileId() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Chat.State = "TBA_sendingNewsletter";
        fixture.Chat["newsletter"] = "news";
        fixture.Controller.Update = new Update {
            Id = 2,
            Message = new Message {
                Id = 2,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                Sticker = new Sticker { FileId = "sticker-file", FileUniqueId = "u1", Width = 100, Height = 100 }
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
        fixture.Http.Enqueue("sendSticker", new Message { Id = 5, Date = DateTime.UtcNow, Chat = new Chat { Id = 2, Type = ChatType.Private } });
        var recipient = new TelegramChat(2) { Type = ChatType.Private };
        await capturedSend!(recipient, TestContext.Current.CancellationToken);

        var stickerRequest = Assert.Single(fixture.Http.Requests, r => r.Method == "sendSticker");
        using var doc = JsonDocument.Parse(stickerRequest.Body!);
        Assert.Equal("sticker-file", doc.RootElement.GetProperty("sticker").GetString());
    }

    // ---- unsupported message type -----------------------------------------------------------------------------

    [Fact]
    public async Task SendNewsletterGetText_UnsupportedMessageType_RepliesUnsupportedAndKeepsState() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Chat.State = "TBA_sendingNewsletter";
        fixture.Chat["newsletter"] = "news";
        fixture.Controller.Update = new Update {
            Id = 2,
            Message = new Message {
                Id = 2,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                Location = new Location { Latitude = 1, Longitude = 1 }
            }
        };
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        fixture.Http.Enqueue("sendMessage", new Message { Id = 3, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendNewsletterGetText();

        Assert.Equal(
            "Only text, photo, audio or sticker is supported as a type of message, please send one of these type",
            LastSendMessageText(fixture.Http));
        Assert.Equal("TBA_sendingNewsletter", fixture.Chat.State);
        await fixture.Service.DidNotReceive().SendNewsletterAsync(
            Arg.Any<string>(), Arg.Any<Func<TelegramChat, CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    // ---- completion reporting ----------------------------------------------------------------------------------

    private static List<string> SendMessageTexts(TestHttpMessageHandler http) {
        var texts = new List<string>();
        foreach (var request in http.Requests.Where(r => r.Method == "sendMessage")) {
            using var doc = JsonDocument.Parse(request.Body!);
            texts.Add(doc.RootElement.GetProperty("text").GetString()!);
        }

        return texts;
    }

    [Fact]
    public async Task SendNewsletterGetText_Completion_RepliesReportAndClearsStateOnly() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Chat.State = "TBA_sendingNewsletter";
        fixture.Chat["newsletter"] = "news";
        fixture.Controller.Update = new Update {
            Id = 2,
            Message = new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }, Text = "hi" }
        };
        fixture.Service.GetNewsletterByKeyAsync("news", Arg.Any<CancellationToken>()).Returns(new Newsletter("news", "desc"));
        var failingChat = new TelegramChat(2) { Type = ChatType.Private };
        fixture.Service.SendNewsletterAsync("news", Arg.Any<Func<TelegramChat, CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(new SendResult(2, new Dictionary<TelegramChat, Exception> { [failingChat] = new InvalidOperationException("boom") }));
        fixture.Http.Enqueue("sendMessage", new Message { Id = 3, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });
        fixture.Http.Enqueue("sendMessage", new Message { Id = 4, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendNewsletterGetText();

        var texts = SendMessageTexts(fixture.Http);
        Assert.Equal("Ok, sending the newsletter...", texts[0]);
        Assert.Equal("Finished - report:\nSuccessful delivery: 2\nFailed delivery: 1", texts[^1]);
        Assert.Null(fixture.Chat.State);
        Assert.Equal("news", fixture.Chat["newsletter"]);
    }

    [Fact]
    public async Task SendNewsletterGetText_NullDataKey_UsesGlobalOverloadAndGlobalWording() {
        var fixture = Create(s => new NewsletterPublishingController<TestTelegramContext>(s));
        fixture.Chat.State = "TBA_sendingNewsletter";
        fixture.Controller.Update = new Update {
            Id = 2,
            Message = new Message { Id = 2, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private }, Text = "hi" }
        };
        fixture.Service.SendNewsletterAsync(Arg.Any<Func<TelegramChat, CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(new SendResult(1, new Dictionary<TelegramChat, Exception>()));
        fixture.Http.Enqueue("sendMessage", new Message { Id = 3, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });
        fixture.Http.Enqueue("sendMessage", new Message { Id = 4, Date = DateTime.UtcNow, Chat = new Chat { Id = 1, Type = ChatType.Private } });

        await fixture.Controller.SendNewsletterGetText();

        var texts = SendMessageTexts(fixture.Http);
        Assert.Equal("Ok, sending the global newsletter...", texts[0]);
        Assert.Equal("Finished - report:\nSuccessful delivery: 1\nFailed delivery: 0", texts[^1]);
        await fixture.Service.Received(1).SendNewsletterAsync(
            Arg.Any<Func<TelegramChat, CancellationToken, Task>>(), Arg.Any<CancellationToken>());
        await fixture.Service.DidNotReceive().SendNewsletterAsync(
            Arg.Any<string>(), Arg.Any<Func<TelegramChat, CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }
}
