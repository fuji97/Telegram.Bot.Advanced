using System;
using JetBrains.Annotations;

namespace Telegram.Bot.Advanced.Extensions; 

public class NewsletterException : Exception {
    public NewsletterException() {
    }

    public NewsletterException([CanBeNull] string? message) : base(message) {
    }

    public NewsletterException([CanBeNull] string? message, [CanBeNull] Exception? innerException) : base(message, innerException) {
    }
}