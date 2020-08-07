using System;
using System.Collections.Generic;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Models {
    public class SendResult {
        public int TotalSubscribers { get; }
        public int TotalErrors { get; }
        public int TotalSuccesses { get; }
        public Dictionary<TelegramChat, Exception> Errors { get; }

        public SendResult(int totalSubscribers, int totalErrors, int totalSuccesses, Dictionary<TelegramChat, Exception> errors) {
            TotalSubscribers = totalSubscribers;
            TotalErrors = totalErrors;
            TotalSuccesses = totalSuccesses;
            Errors = errors;
        }
    }
}