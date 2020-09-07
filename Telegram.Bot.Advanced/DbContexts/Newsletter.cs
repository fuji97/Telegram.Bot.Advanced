using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Telegram.Bot.Advanced.DbContexts {
    public class Newsletter {
        public Newsletter(string key, string description) {
            Key = key;
            Description = description;
        }

        public Newsletter() {
        }

        [Key]
        public string Key { get; set; }
        public string Description { get; set; }
        
        [JsonIgnore]
        public List<NewsletterChat> NewsletterChats { get; set; }
    }
}