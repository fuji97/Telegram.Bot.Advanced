using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Exceptions;

namespace Telegram.Bot.Advanced.Core.Tools {
    public class InlineDataWrapper {
        public string Command { get; set; }
        public Dictionary<string, string> Data { get; set; }

        public InlineDataWrapper(string command, Dictionary<string, string> data = null) {
            Command = command;
            Data = data ?? new Dictionary<string, string>();
        }

        public override string ToString() {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            foreach (var element in Data) {
                queryString[element.Key] = element.Value;
            }

            var result = HttpUtility.UrlEncode(Command) + '&' + queryString.ToString();
            if (System.Text.Encoding.UTF8.GetByteCount(result) > 64) {
                throw new MaximumSizeExceededException("The combined size of Command and Data exceeds the maximum 64-bytes limit of CallbackData imposed by Telegram.");
            }
            
            return result;
        }

        public static InlineDataWrapper ParseInlineData(string inlineData) {
            string command = null;
            Dictionary<string, string> data = null;
            var matches = Regex.Matches(inlineData, "^(.*?)&(.*)$");

            if (matches[1].Success) {
                command = HttpUtility.UrlDecode(matches[1].Value);
            }

            if (matches[2].Success) {
                var rawData = matches[2].Value;
                var query = HttpUtility.ParseQueryString(rawData);

                data = query.AllKeys.ToDictionary(k => k, k => query[k]);
            }
            
            return new InlineDataWrapper(command, data);
        }
    }
}