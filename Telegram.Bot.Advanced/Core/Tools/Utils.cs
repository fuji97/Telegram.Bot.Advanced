using System.Text.RegularExpressions;

namespace Telegram.Bot.Advanced.Core.Tools;

public static partial class Utils {
    /// <summary>
    /// Redacts every Telegram-bot-token-shaped substring (digits followed by ':' and a non-whitespace secret) in
    /// <paramref name="text"/>. This library never logs a bot token itself; this helper is exposed for host
    /// applications that want to sanitize their own log lines or exception messages before writing them, in case
    /// a token ends up embedded in text originating outside this library (e.g. a future Telegram.Bot exception
    /// message or a host-authored log statement).
    /// </summary>
    public static string ObfuscateToken(string text) {
        return TokenRegex().Replace(text, "00000000:XXXXXXXXXX");
    }

    [GeneratedRegex(@"\d+:\S+")]
    private static partial Regex TokenRegex();
}