using System.Collections.Generic;

namespace Telegram.Bot.Advanced.Extensions {
    public class MessageCommand {
        public string Command { get; set; }
        public string Target { get; set; }
        public List<string> Parameters { get; set; } = new List<string>();
        public string Text { get; set; }

        public bool IsCommand() {
            return Command != null; 
        }
    }
}