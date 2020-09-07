using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Extensions {
    public static class UpdateExtensions {
        public static Message GetMessage(this Update update) {
            switch (update.Type) {
                case UpdateType.Message:
                    return update.Message;
                case UpdateType.CallbackQuery:
                    return update.CallbackQuery.Message;
                case UpdateType.EditedMessage:
                    return update.EditedMessage;
                default:
                    return null;
            }
        }
    }
}