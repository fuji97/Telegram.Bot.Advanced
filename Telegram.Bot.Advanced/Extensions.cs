﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced
{
    public static class Extensions {
        public static MessageCommand GetCommand(this Message message) {
            if (message.Text == null)
                return new MessageCommand();

            var match = Regex.Match(message.Text, @"^\/([^@\s]+)@?(?:(\S+)|)\s?([\s\S]*)$");
            
            var command = new MessageCommand() {
                Command = match.Groups[1].Value,
                Target = match.Groups[2].Value,
                Parameters = new List<string>(match.Groups[3].Value.Split(" "))
            };

            return command;
        }
    }

    public class MessageCommand {
        public string Command { get; set; } = null;
        public string Target { get; set; } = null;
        public List<string> Parameters { get; set; } = new List<string>();

        public bool IsCommand() {
            return Command != null; 
        }
    }
}
