namespace Telegram.Bot.Advanced.Exceptions;

/// <summary>
/// Thrown when a computed value (e.g. combined callback command and data) exceeds a size limit imposed by Telegram
/// </summary>
public sealed class MaximumSizeExceededException : ArgumentOutOfRangeException {
    public MaximumSizeExceededException() {
    }

    public MaximumSizeExceededException(string message) : base(paramName: null, message: message) {
    }

    public MaximumSizeExceededException(string message, Exception innerException) : base(message, innerException) {
    }
}
