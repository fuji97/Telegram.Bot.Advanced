using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Test
{
    class Master
    {
        [Key]
        public TelegramChat User { get; set; }
        [Key]
        public string Name { get; set; }
        public string FriendCode { get; set; }
        public string SupportList { get; set; }
        public string ServantList { get; set; }
        public MasterServer Server { get; set; }
    }

    internal enum MasterServer {
        US = 0,
        JP = 0
    }
}
