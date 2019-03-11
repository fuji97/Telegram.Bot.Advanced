using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Models
{
    public class TelegramChat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public string Username { get; set; }
        public int State { get; set; } = 0;
        public ChatType Type { get; set; }
        public ChatRole Role { get; set; } = ChatRole.User;
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool AllMembersAreAdministrators { get; set; } = false;
        public string Description { get; set; }
        public string InviteLink { get; set; }
        public string StickerSetName { get; set; }
        public bool? CanSetStickerSet { get; set; }
        public ICollection<Data> Data { get; set; } = new List<Data>();

        public TelegramChat() {
        }

        public TelegramChat(long id) {
            Id = id;
        }

        public TelegramChat(Chat update) {
            Id = update.Id;
            Username = update.Username;
            Type = update.Type;
            Title = update.Title;
            FirstName = update.FirstName;
            LastName = update.LastName;
            AllMembersAreAdministrators = update.AllMembersAreAdministrators;
            Description = update.Description;
            InviteLink = update.InviteLink;
            StickerSetName = update.StickerSetName;
            CanSetStickerSet = update.CanSetStickerSet;
        }

        public void AddData(string key, string value) {
            if (Data.Count(d => d.Key == key) > 0) {
                throw new DuplicateDataKeyException();
            }

            Data.Add(new Data(this, key, value));
        }

        public void UpdateData(string key, string value) {
            var data = Data.FirstOrDefault(d => d.Key == key);
            if (data != null) {
                data.Value = value;
            }
            else {
                Data.Add(new Data(this, key, value));
            }
        }

        public string this[string key]
        {
            get {
                return Data.FirstOrDefault(d => d.Key == key)?.Value;
            }
            set {
                var anotherName = Data.FirstOrDefault(d => d.Key == key);
                if (anotherName != null) {
                    anotherName.Value = value;
                }
                else
                {
                    Data.Add(new Data(this, key, value));
                }
            }
        }

        public void Update(TelegramContext context) {
            //context.Update(this);
            //context.SaveChanges();
        }

        public bool Add(TelegramContext context) {
            var user = context.Users.FirstOrDefault(u => u.Id == Id);
            if (user == null) {
                context.Add(this);
                return true;
            }

            return false;
        }

        public async Task<bool> AddAsync(TelegramContext context)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == Id);
            if (user == null)
            {
                await context.AddAsync(this);
                return true;
            }

            return false;
        }

        public static TelegramChat Get(TelegramContext context, long id) {
            var user = context.Users.Include(chat => chat.Data).FirstOrDefault(u => u.Id == id);
            return user;
        }

        public static async Task<TelegramChat> GetAsync(TelegramContext context, long id)
        {
            var user = await context.Users.Include(chat => chat.Data).FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }

        public override bool Equals(Object obj) {
            if (obj is TelegramChat chat)
                return Id == chat.Id;
            return false;
        }

       

        /*
        public bool IsDifferent(User user) {
            return !(Id != user.Id 
                || Username != user.Username 
                || State != user.State 
                || Role != user.Role 
                || !Data.SequenceEqual(user.Data));
        }
        */

    }

    

    public enum ChatRole {
        User = 0,
        Blocked = 1,
        Banned = 2,
        Moderator = 3,
        Administrator = 4
    }
}
