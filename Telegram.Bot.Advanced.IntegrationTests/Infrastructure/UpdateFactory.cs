using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.IntegrationTests.Infrastructure;

/// <summary>
/// Builds <see cref="Update"/> payloads for synthetic webhook/dispatch tests.
/// </summary>
public static class UpdateFactory {
    public static Update TextMessage(
        long chatId,
        string text,
        int updateId = 1,
        long? senderId = null,
        ChatType chatType = ChatType.Private,
        string? chatUsername = null) => new() {
        Id = updateId,
        Message = new Message {
            Id = updateId,
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = chatId, Type = chatType, Username = chatUsername },
            From = new User { Id = senderId ?? chatId, FirstName = "IntegrationUser" },
            Text = text
        }
    };

    public static string Serialize(Update update) => JsonSerializer.Serialize(update, JsonBotAPI.Options);
}
