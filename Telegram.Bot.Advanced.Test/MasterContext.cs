using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Test
{
    class MasterContext : UserContext {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=localhost\SQLEXPRESS;Database=telegram-bot;Trusted_Connection=True;");
        }

        public DbSet<Master> Masters { get; set; }
    }
}
