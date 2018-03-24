using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Telegram.Bot.Advanced.Models
{
    public class Data {
        [Key]
        public User User { get; set; }
        [Key]
        public string Key { get; set; }
        public string Value { get; set; }

        public override bool Equals(object obj) {
            if (obj is Data data) {
                return User == data.User && Key == data.Key && Value == data.Value;
            }
            return false;
        }

        public static bool operator ==(Data left, Data right) {
            return left != null && left.Equals(right);
        }

        public static bool operator !=(Data left, Data right)
        {
            return left == null || !left.Equals(right);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (User != null ? User.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
