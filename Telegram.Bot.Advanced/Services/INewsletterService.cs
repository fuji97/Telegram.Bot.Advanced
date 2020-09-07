using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Services {
    public interface INewsletterService {
        /// <summary>
        /// Execute the action sendAction asynchronously for each subscribed chat. The TelegramChat parameter in the 
        /// action change on every iteration for every chat subscribed to the newsletter.
        /// </summary>
        /// <param name="newsletterKey"></param>
        /// <param name="sendAction"></param>
        /// <returns>The result of the operation</returns>
        Task<SendResult> SendNewsletterAsync(string newsletterKey, Func<TelegramChat, Task> sendAction);
        
        /// <summary>
        /// Execute the action sendAction for each subscribed chat. The TelegramChat parameter in the 
        /// action change on every iteration for every chat subscribed to the newsletter.
        /// </summary>
        /// <param name="newsletterKey"></param>
        /// <param name="sendAction"></param>
        /// <returns>The result of the operation</returns>
        SendResult SendNewsletter(string newsletterKey, Action<TelegramChat> sendAction);
        
        /// <summary>
        /// Execute the action sendAction asynchronously for each chat. The TelegramChat parameter in the 
        /// action change on every iteration for every chat.
        /// </summary>
        /// <param name="sendAction"></param>
        /// <returns>The result of the operation</returns>
        Task<SendResult> SendNewsletterAsync(Func<TelegramChat, Task> sendAction);
        
        /// <summary>
        /// Execute the action sendAction for each chat. The TelegramChat parameter in the 
        /// action change on every iteration for every chat.
        /// </summary>
        /// <param name="sendAction"></param>
        /// <returns>The result of the operation</returns>
        SendResult SendNewsletter(Action<TelegramChat> sendAction);
        
        /// <summary>
        /// Subscribe the chat to the newsletter asynchronously.
        /// </summary>
        /// <param name="newsletterKey"></param>
        /// <param name="chatId"></param>
        /// <returns></returns>
        Task<bool> SubscribeChatAsync(string newsletterKey, long chatId);
        
        /// <summary>
        /// Subscribe the chat to the newsletter asynchronously.
        /// </summary>
        /// <param name="newsletterKey"></param>
        /// <param name="chatId"></param>
        /// <returns></returns>
        bool SubscribeChat(string newsletterKey, long chatId);
        Task<bool> UnsubscribeChatAsync(string newsletterKey, long chatId);
        bool UnsubscribeChat(string newsletterKey, long chatId);

        Task<bool> IsChatSubscribedToNewsletterAsync(string newsletterKey, long chatId);
        bool IsChatSubscribedToNewsletter(string newsletterKey, long chatId);
        Task<List<Newsletter>> GetNewslettersAsync();
        List<Newsletter> GetNewsletters();
        Task<Newsletter> GetNewsletterByKeyAsync(string newsletterKey);
        Newsletter GetNewsletterByKey(string newsletterKey);
        Task<bool> CreateNewsletterAsync(Newsletter newsletter);
        bool CreateNewsletter(Newsletter newsletter);

        Task<bool> RemoveNewsletterAsync(string newsletterKey);
        
        bool RemoveNewsletter(string newsletterKey);
    }
}