using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Models;

public sealed partial class MessageCommand {
    public string? Command { get; set; }
    public string? Target { get; set; }
    public List<string> Parameters { get; set; } = [];
    public string? Text { get; set; }
    public string? Message { get; set; }

    public bool IsCommand() => Command != null;

    public bool IsEmpty() => Text == null;

    public MessageCommand() {}

    public MessageCommand(Message message) {
        if (message.Text == null)
            return;

        var match = CommandRegex().Match(message.Text);

        Text = message.Text;
        Command = match.Groups[1].Success ? match.Groups[1].Value : null;
        Target = match.Groups[2].Success ? match.Groups[2].Value : null;
        Message = match.Groups[3].Success ? match.Groups[3].Value : null;
        Parameters = [.. match.Groups[3].Value.Split(" ").Where(s => s != "")];
    }

    [GeneratedRegex(@"^\/([^@\s]+)@?(?:(\S+)|)\s?([\s\S]*)$")]
    private static partial Regex CommandRegex();
}
