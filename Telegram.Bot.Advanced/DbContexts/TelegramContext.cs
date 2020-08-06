using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.DbContexts
{
    /// <summary>
    /// Extension of DbContext to include data structures used internally by this framework
    /// </summary>
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

            modelBuilder.Entity<NewsletterChat>()
                .HasKey(t => new {t.NewsletterKey, t.ChatId});
            modelBuilder.Entity<NewsletterChat>()
                .HasOne(nc => nc.Chat)
                .WithMany(c => c.NewsletterChats)
                .HasForeignKey(nc => nc.ChatId);
            modelBuilder.Entity<NewsletterChat>()
                .HasOne(nc => nc.Newsletter)
                .WithMany(n => n.NewsletterChats)
                .HasForeignKey(nc => nc.NewsletterKey);
        }

        // Entities
        public DbSet<TelegramChat> Users { get; set; }
        public DbSet<Data> Data { get; set; }
        public DbSet<Newsletter> Newsletters { get; set; }
        public DbSet<NewsletterChat> NewsletterChats { get; set; }
    }
}
