using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Telegram.Bot.Advanced.Models {
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
        
        public List<NewsletterChat> NewsletterChats { get; set; }
    }
}