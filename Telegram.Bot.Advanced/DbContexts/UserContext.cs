using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.DbContexts
{
    public class UserContext : DbContext {
        public UserContext () {}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseSqlServer(@"Server=localhost\SQLEXPRESS;Database=telegram-bot;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {}

        // Entities
        public DbSet<TelegramChat> Users { get; set; }
        public DbSet<Data> Data { get; set; }
    }
}
