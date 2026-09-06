using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Extensions;

public static class TelegramBotExtensions {
    public static TelegramChat ToModel(this User user) => new() {
        Id = user.Id,
        Username = user.Username,
        FirstName = user.FirstName,
        LastName = user.LastName
    };

    public static bool IsGroup(this Chat chat) => chat.Type is ChatType.Group or ChatType.Supergroup;
}