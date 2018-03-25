using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
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
        public List<Data> Data { get; set; } = new List<Data>();

        public TelegramChat() {
        }

        public TelegramChat(long id) {
            Id = id;
        }

        public TelegramChat(Chat update) {
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

        public void Update() {
            using (var context = new UserContext()) {

                context.Update(this);
                context.SaveChanges();
            }
        }

        public bool Add() {
            using (var context = new UserContext()) {
                var user = context.Users.FirstOrDefault(u => u.Id == Id);
                if (user == null) {
                    context.Add(this);
                    return true;
                }

                return false;
            }
        }

        public static TelegramChat Get(long id) {
            using (var context = new UserContext())
            {
                var user = context.Users.FirstOrDefault(u => u.Id == id);
                return user;
            }
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
