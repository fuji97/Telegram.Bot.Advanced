using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Test
{
    public class RegisteredChat {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MasterId { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ChatId { get; set; }
        [ForeignKey("MasterId")]
        public Master Master { get; set; }
        [ForeignKey("ChatId")]
        public TelegramChat Chat { get; set; }

        public RegisteredChat() { }

        public RegisteredChat(int masterId, long chatId) {
            MasterId = masterId;
            ChatId = chatId;
        }
    }
}
