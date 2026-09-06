using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Tests.Infrastructure;

/// <summary>
/// Builds a real <see cref="Dispatcher{TContext}"/> (the actual subject under test in DispatcherRoutingTests) wired
/// to an NSubstitute <see cref="ITelegramBotData"/> whose Bot is always a real Telegram.Bot.TelegramBotClient,
/// plus a DI container exposing the EF InMemory <see cref="TestTelegramContext"/> and every registered controller.
/// </summary>
public static class DispatcherTestHarness {
    public sealed record Harness(
        Dispatcher<TestTelegramContext> Dispatcher,
        IServiceProvider Provider,
        TestHttpMessageHandler HttpHandler,
        ITelegramBotData BotData,
        string DatabaseName);

    public static Harness Create(
        IList<Type> controllers,
        string? username = "test_bot",
        UserUpdate userUpdate = UserUpdate.EveryMessage,
        IgnoreBehaviour groupChatBehaviour = IgnoreBehaviour.IgnoreNothing,
        IgnoreBehaviour privateChatBehaviour = IgnoreBehaviour.IgnoreNothing,
        IReadOnlyList<UserRole>? defaultUserRole = null,
        string? databaseName = null) {
        var (client, httpHandler) = TestBotClientFactory.Create();

        var botData = Substitute.For<ITelegramBotData>();
        botData.Bot.Returns(client);
        botData.Username = username;
        botData.UserUpdate.Returns(userUpdate);
        botData.GroupChatBehaviour.Returns(groupChatBehaviour);
        botData.PrivateChatBehaviour.Returns(privateChatBehaviour);
        botData.DefaultUserRole.Returns(defaultUserRole ?? []);

        var dispatcher = new Dispatcher<TestTelegramContext>(botData, controllers);

        var dbName = databaseName ?? Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<TestTelegramContext>(o => o.UseInMemoryDatabase(dbName));
        dispatcher.RegisterController(services);

        var provider = services.BuildServiceProvider();

        return new Harness(dispatcher, provider, httpHandler, botData, dbName);
    }
}
