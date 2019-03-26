using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Models {
    public class MessageCommand {
        public string Command { get; set; }
        public string Target { get; set; }
        public List<string> Parameters { get; set; } = new List<string>();
        public string Text { get; set; }
        public string Message { get; set;}

        public bool IsCommand() {
            return Command != null; 
        }
        
        public MessageCommand() {}

        public MessageCommand(Message message) {
            if (message.Text == null)
                return;

            var match = Regex.Match(message.Text, @"^\/([^@\s]+)@?(?:(\S+)|)\s?([\s\S]*)$");

            Text = message.Text;
            Command = match.Groups[1].Success ? match.Groups[1].Value : null;
            Target = match.Groups[2].Success ? match.Groups[2].Value : null;
            Message = match.Groups[3].Success ? match.Groups[3].Value : null;
            Parameters = new List<string>(match.Groups[3].Value.Split(" ").Where(s => s != ""));
        }
    }
}