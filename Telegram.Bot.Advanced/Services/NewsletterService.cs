using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Exceptions;

namespace Telegram.Bot.Advanced.Services {
    public class NewsletterService<TDbContext> : INewsletterService where TDbContext : TelegramContext {
        private readonly TDbContext _context;
        private readonly ILogger<NewsletterService<TDbContext>> _logger;

        public NewsletterService(TDbContext context, ILogger<NewsletterService<TDbContext>> logger) {
            _context = context;
            _logger = logger;
        }
        
        private IQueryable<TelegramChat> GetChatFromNewsletterKey(string newsletterKey) {
            return _context.NewsletterChats
                .Include(nc => nc.Chat)
                .Where(nc => nc.NewsletterKey == newsletterKey)
                .Select(nc => nc.Chat);
        }
        
        private async Task<Newsletter> GetNewsletter(string newsletterKey) {
            var newsletter = await _context.Newsletters.FindAsync(newsletterKey);
            if (newsletter == null)
                throw new InvalidParameterException(nameof(newsletterKey), $"The newsletter {newsletter} doesn't exists");
            return newsletter;
        }
        
        private async Task<TelegramChat> GetTelegramChat(long chatId) {
            var chat = await _context.Users.FindAsync(chatId);
            if (chat == null)
                throw new InvalidParameterException(nameof(chatId), $"The chat {chat} doesn't exists");
            return chat;
        }

        /// <inheritdoc />
        public async Task<SendResult> SendNewsletterAsync(string newsletterKey, Func<TelegramChat, Task> sendAction) {
            var result = await GetChatFromNewsletterKey(newsletterKey)
                .ToListAsync();

            var successes = 0;
            Dictionary<TelegramChat, Exception> errors = new Dictionary<TelegramChat, Exception>();
            
            foreach (var chat in result) {
                try {
                    await sendAction(chat);
                    successes++;
                }
                catch (Exception e) {
                    errors.Add(chat, e);
                }
            }

            return new SendResult(successes + errors.Count, errors.Count, successes, errors);
        }

        /// <inheritdoc />
        public SendResult SendNewsletter(string newsletterKey, Action<TelegramChat>  sendAction) {
            var result = GetChatFromNewsletterKey(newsletterKey)
                .ToList();

            var successes = 0;
            Dictionary<TelegramChat, Exception> errors = new Dictionary<TelegramChat, Exception>();

            foreach (var chat in result) {
                try {
                    sendAction(chat);
                    successes++;
                }
                catch (Exception e) {
                    errors.Add(chat, e);
                }
            }
            
            return new SendResult(successes + errors.Count, errors.Count, successes, errors);
        }


        /// <inheritdoc />
        public async Task<bool> SubscribeChatAsync(string newsletterKey, long chatId) {
            var newsletter = await GetNewsletter(newsletterKey);
            var chat = await GetTelegramChat(chatId);

            if (!await _context.NewsletterChats
                .AnyAsync(nc => nc.NewsletterKey == newsletterKey && nc.ChatId == chatId)) {
                await _context.AddAsync(new NewsletterChat(newsletter, chat));
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool SubscribeChat(string newsletterKey, long chatId) {
            var newsletter = GetNewsletter(newsletterKey).Result;
            var chat = GetTelegramChat(chatId).Result;

            if (!_context.NewsletterChats
                .Any(nc => nc.NewsletterKey == newsletterKey && nc.ChatId == chatId)) {
                _context.Add(new NewsletterChat(newsletter, chat));
                _context.SaveChanges();
                return true;
            }

            return false;
        }

        public async Task<bool> UnsubscribeChatAsync(string newsletterKey, long chatId) {
            var newsletter = await GetNewsletter(newsletterKey);
            var chat = await GetTelegramChat(chatId);

            var newsletterChat = await _context.NewsletterChats
                .FindAsync(new {newsletterKey, chatId});

            if (newsletterChat == null) return false;

            _context.Remove(newsletterChat);
            await _context.SaveChangesAsync();

            return true;
        }

        public bool UnsubscribeChat(string newsletterKey, long chatId) {
            var newsletter = GetNewsletter(newsletterKey).Result;
            var chat = GetTelegramChat(chatId).Result;

            var newsletterChat = _context.NewsletterChats
                .Find(new {newsletterKey, chatId});

            if (newsletterChat == null) return false;

            _context.Remove(newsletterChat);
            _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsChatSubscribedToNewsletterAsync(string newsletterKey, long chatId) {
            var newsletter = await GetNewsletter(newsletterKey);
            var chat = await GetTelegramChat(chatId);

            return await _context.NewsletterChats
                .AnyAsync(nc => nc.NewsletterKey == newsletterKey && nc.ChatId == chatId);
        }

        public bool IsChatSubscribedToNewsletter(string newsletterKey, long chatId) {
            var newsletter = GetNewsletter(newsletterKey).Result;
            var chat = GetTelegramChat(chatId).Result;

            return _context.NewsletterChats
                .Any(nc => nc.NewsletterKey == newsletterKey && nc.ChatId == chatId);
        }

        public async Task<Newsletter> GetNewsletterByKeyAsync(string newsletterKey) {
            return await _context.Newsletters
                .FirstOrDefaultAsync(n => n.Key == newsletterKey);
        }

        public Newsletter GetNewsletterByKey(string newsletterKey) {
            return _context.Newsletters
                .FirstOrDefault(n => n.Key == newsletterKey);
        }

        public async Task<List<Newsletter>> GetNewslettersAsync() {
            return await _context.Newsletters.ToListAsync();
        }

        public List<Newsletter> GetNewsletters() {
            return _context.Newsletters.ToList();
        }

        public async Task<bool> CreateNewsletterAsync(Newsletter newsletter) {
            if (!await _context.Newsletters.AnyAsync(n => n.Key == newsletter.Key)) {
                await _context.AddAsync(newsletter);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public bool CreateNewsletter(Newsletter newsletter) {
            if (!_context.Newsletters.Any(n => n.Key == newsletter.Key)) {
                _context.Add(newsletter);
                _context.SaveChanges();
                return true;
            }

            return false;
        }

        public async Task<bool> RemoveNewsletterAsync(string newsletterKey) {
            var newsletter = await _context.Newsletters.FindAsync(newsletterKey);

            if (newsletter == null) return false;

            _context.Remove(newsletter);
            await _context.SaveChangesAsync();

            return true;
        }

        public bool RemoveNewsletter(string newsletterKey) {
            var newsletter = _context.Newsletters.Find(newsletterKey);

            if (newsletter == null) return false;

            _context.Remove(newsletter);
            _context.SaveChanges();

            return true;
        }
    }
    
    public static class NewsletterServiceExtensions {
        public static IServiceCollection AddNewsletter<TDbContext>(this IServiceCollection services) where TDbContext : TelegramContext {
            services.AddScoped<INewsletterService, NewsletterService<TDbContext>>();
            return services;
        }
    }
}