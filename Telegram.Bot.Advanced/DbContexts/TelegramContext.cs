using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.DbContexts
{
    public abstract class TelegramContext : DbContext {
        public TelegramContext() {}

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Data>().HasKey(t => new { t.UserId, t.Key });
        }

        // Entities
        public DbSet<TelegramChat> Users { get; set; }
        public DbSet<Data> Data { get; set; }
    }
}
