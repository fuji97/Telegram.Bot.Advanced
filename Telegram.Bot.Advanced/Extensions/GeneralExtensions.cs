using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Extensions
{
    public static class GeneralExtensions {
        public static MessageCommand GetCommand(this Message message) {
            if (message.Text == null)
                return new MessageCommand();

            var match = Regex.Match(message.Text, @"^\/([^@\s]+)@?(?:(\S+)|)\s?([\s\S]*)$");
            
            var command = new MessageCommand {
                Text = message.Text,
                Command = match.Groups[1].Success ? match.Groups[1].Value : null,
                Target = match.Groups[2].Success ? match.Groups[2].Value : null,
                Parameters = new List<string>(match.Groups[3].Value.Split(" ").Where(s => s != ""))
            };

            return command;
        }
    }
}
