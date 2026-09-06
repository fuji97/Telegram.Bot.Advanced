using System.Text.Json.Serialization;

namespace Telegram.Bot.Advanced.DbContexts;

public sealed class Data : IEquatable<Data> {
    public long UserId { get; set; }
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    [JsonIgnore]
    public TelegramChat Chat { get; set; } = null!;

    public Data() { }

    public Data(TelegramChat chat, string key, string? value) {
        Chat = chat;
        UserId = chat.Id;
        Key = key;
        Value = value;
    }

    public bool Equals(Data? other) {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        return UserId == other.UserId && Key == other.Key && Value == other.Value;
    }

    public override bool Equals(object? obj) => obj is Data other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(UserId, Key, Value);

    public static bool operator ==(Data? left, Data? right) {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Data? left, Data? right) => !(left == right);
}
