using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Test
{
    public class Master
    {
        public TelegramChat User { get; set; }
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string FriendCode { get; set; }
        public string SupportList { get; set; }
        public string ServantList { get; set; }
        public MasterServer Server { get; set; }
        public MasterStatus Status { get; set; }
        public ICollection<RegisteredChat> RegisteredChats { get; set; }

        public Master() { }

        public Master(TelegramChat user, string name, string friendCode, MasterServer server, string support = null, string servant = null,
            MasterStatus status = MasterStatus.Enabled) {
            User = user;
            Name = name;
            FriendCode = friendCode;
            Server = server;
            SupportList = support;
            ServantList = servant;
            Status = status;
        }

        /*
        public void UpdateData(string key, string value)
        {
            var data = Data.FirstOrDefault(d => d.Key == key);
            if (data != null)
            {
                data.Value = value;
            }
            else
            {
                Data.Add(new Data(this, key, value));
            }
        }
        */
    }

    public enum MasterStatus {
        Enabled = 0,
        Disabled = 1
    }

    public enum MasterServer {
        US = 0,
        JP = 1
    }
}
