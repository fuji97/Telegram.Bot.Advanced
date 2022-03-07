using System;
using JetBrains.Annotations;

namespace Telegram.Bot.Advanced.Extensions; 

public class NewsletterException : Exception {
    public NewsletterException() {
    }

    public NewsletterException(string? message) : base(message) {
    }

    public NewsletterException(string? message, Exception? innerException) : base(message, innerException) {
    }
}