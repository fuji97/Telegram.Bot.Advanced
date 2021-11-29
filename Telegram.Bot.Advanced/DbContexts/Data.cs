using Newtonsoft.Json;

namespace Telegram.Bot.Advanced.DbContexts
{
    public class Data {
        public long UserId { get; set; }
        public string Key { get; set; }
        public string? Value { get; set; }
        [JsonIgnore]
        public TelegramChat Chat { get; set; }

        public Data() { }

        public Data(TelegramChat user, string key, string? value) {
            UserId = user.Id;
            Key = key;
            Value = value;
        }

        public override bool Equals(object obj) {
            if (obj is Data data) {
                return UserId == data.UserId && Key == data.Key && Value == data.Value;
            }
            return false;
        }

        protected bool Equals(Data other) {
            return UserId == other.UserId && string.Equals(Key, other.Key) && string.Equals(Value, other.Value);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = UserId.GetHashCode();
                hashCode = (hashCode * 397) ^ (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(Data? left, Data? right) {
            if (ReferenceEquals(null, left) && ReferenceEquals(null, right))
            {
                return false;
            }

            if (ReferenceEquals(null, left) || ReferenceEquals(null, right))
            {
                return true;
            }
            return left.Equals(right);
        }

        public static bool operator !=(Data? left, Data? right)
        {
            if (ReferenceEquals(null, left) && ReferenceEquals(null, right)) {
                return false;
            }

            if (ReferenceEquals(null, left) || ReferenceEquals(null, right)) {
                return true;
            }
            return !left.Equals(right);
        }
    }
}
