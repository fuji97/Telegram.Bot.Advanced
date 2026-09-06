using System.Diagnostics.CodeAnalysis;

namespace Telegram.Bot.Advanced.Core.Holder;

public interface ITelegramHolder : IEnumerable<ITelegramBotData> {
    /// <summary>
    /// Attempts to find the bot registered for the given endpoint.
    /// </summary>
    bool TryGet(string endpoint, [NotNullWhen(true)] out ITelegramBotData? bot);
}
