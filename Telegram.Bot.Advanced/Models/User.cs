using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public string Username { get; set; }
        public int State { get; set; } = 0;
        public UserRole Role { get; set; } = UserRole.User;
        public List<Data> Data { get; set; }

        public User() { }

        public User(long id, string username = null) {
            Id = id;
            if (username != null)
                Username = username;
        }

        public void Update() {
            using (var context = new UserContext())
            {
                var user = context.Users.FirstOrDefault(u => u.Id == Id);
                if (user != null) {
                    context.Update(this);
                }
                else {
                    context.Add(this);
                }
                context.SaveChanges();
            }
        }

        public static User Get(long id) {
            using (var context = new UserContext())
            {
                var user = context.Users.FirstOrDefault(u => u.Id == id);
                return user;
            }
        }

        public bool Equals(User obj) {
            return Id == obj.Id;
        }

        public bool IsDifferent(User user) {
            return !(Id != user.Id 
                || Username != user.Username 
                || State != user.State 
                || Role != user.Role 
                || !Data.SequenceEqual(user.Data));
        }
    }

    public enum UserRole {
        User = 0,
        Blocked = 1,
        Banned = 2,
        Moderator = 3,
        Administrator = 4
    }
}
