using System.Text.RegularExpressions;

namespace Telegram.Bot.Advanced.Core.Tools; 

public static class Utils {
    public static string ObfuscateToken(string text) {
        return Regex.Replace(text, @"\d+:.{35}", "00000000:XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
    }
}