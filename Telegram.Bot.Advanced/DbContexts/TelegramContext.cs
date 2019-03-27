using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.DbContexts
{
    public abstract class TelegramContext : DbContext {
        
        protected TelegramContext(DbContextOptions options)
            : base(options) {
        }
        
        protected TelegramContext() {}
        
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Data>().HasKey(t => new { t.UserId, t.Key });
            modelBuilder.Entity<TelegramChat>()
                .HasMany(c => c.Data)
                .WithOne(c => c.Chat)
                .HasForeignKey(c => c.UserId);
        }

        // Entities
        public DbSet<TelegramChat> Users { get; set; }
        public DbSet<Data> Data { get; set; }
    }
}
