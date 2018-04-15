using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Telegram.Bot.Advanced.Models
{
    public class Data {
        [Key]
        public long UserId { get; set; }
        [Key]
        public string Key { get; set; }
        public string Value { get; set; }
        [ForeignKey("UserId")]
        public TelegramChat User { get; set; }

        public Data() { }

        public Data(TelegramChat user, string key, string value) {
            UserId = user.Id;
            User = user;
            Key = key;
            Value = value;
        }

        public override bool Equals(object obj) {
            if (obj is Data data) {
                return UserId == data.UserId && Key == data.Key && Value == data.Value;
            }
            return false;
        }

        public static bool operator ==(Data left, Data right) {
            if (ReferenceEquals(null, left) && ReferenceEquals(null, right))
            {
                return false;
            }
            else if (ReferenceEquals(null, left) || ReferenceEquals(null, right))
            {
                return true;
            }
            return left.Equals(right);
        }

        public static bool operator !=(Data left, Data right)
        {
            if (ReferenceEquals(null, left) && ReferenceEquals(null, right)) {
                return false;
            } else if (ReferenceEquals(null, left) || ReferenceEquals(null, right)) {
                return true;
            }
            return !left.Equals(right);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (UserId.GetHashCode());
                hashCode = (hashCode * 397) ^ (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
