using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Controller {
    public class NewsletterController<TContext> : TelegramController<TContext> where TContext : TelegramContext {
        private const string SendingNewsletterState = "TBA_sendingNewsletter";
        
        private readonly TContext _context;
        private readonly ILogger<NewsletterController<TContext>> _logger;
        private readonly INewsletterService _newsletterService;

        public NewsletterController(TContext context, ILogger<NewsletterController<TContext>> logger, INewsletterService newsletterService) {
            _context = context;
            _logger = logger;
            _newsletterService = newsletterService;
        }

        [CommandFilter("subscribe")]
        public async Task SubscribeToNewsletter() {
            if (!await CheckIfAdminInGroups()) return;

            if (MessageCommand.Parameters.Count != 1) {
                await ReplyTextMessageAsync("Usage:\n/subscribe <newsletter>");
            }
            var newsletterKey = MessageCommand.Parameters[0];

            var newsletters = await _newsletterService.GetNewslettersAsync();

            if (newsletters.Any(n => n.Key == newsletterKey)) {
                var result = await _newsletterService.SubscribeChatAsync(newsletterKey, TelegramChat!.Id);
                if (result) {
                    await ReplyTextMessageAsync(
                        $"Successfully subscribed to the {newsletterKey} newsletter");
                }
                else {
                    await ReplyTextMessageAsync(
                        "Can't subscribe to newsletter, probably you are already subscribed");
                }
            }
            else {
                await ReplyTextMessageAsync(
                    $"The newsletter {newsletterKey} doesn't exist.");
            }
        }

        [CommandFilter("unsubscribe")]
        public async Task UnsubscribeFromNewsletter() {
            if (!await CheckIfAdminInGroups()) return;

            if (MessageCommand.Parameters.Count != 1) {
                await ReplyTextMessageAsync("Usage:\n/unsubscribe <newsletter>");
            }
            var newsletterKey = MessageCommand.Parameters[0];

            var newsletters = await _newsletterService.GetNewslettersAsync();

            if (newsletters.Any(n => n.Key == newsletterKey)) {
                if (await _newsletterService.IsChatSubscribedToNewsletterAsync(newsletterKey, TelegramChat!.Id)) {
                    var result = await _newsletterService.SubscribeChatAsync(newsletterKey, TelegramChat.Id);
                    if (result) {
                        await ReplyTextMessageAsync(
                            $"Successfully unsubscribed from the {newsletterKey} newsletter");
                    }
                    else {
                        await ReplyTextMessageAsync(
                            "Can't unsubscribe from newsletter");
                    }
                }
            }
            else {
                await ReplyTextMessageAsync(
                    $"The newsletter {newsletterKey} doesn't exist.");
            }
        }

        [ChatRoleFilter(ChatRole.Administrator, ChatRole.Moderator), CommandFilter("send_newsletter")]
        public async Task SendNewsletter() {
            if (MessageCommand.Parameters.Count != 1) {
                await ReplyTextMessageAsync("Usage:\n/send_newsletter <newsletter>");
                return;
            }

            var newsletterKey = MessageCommand.Parameters[0];
            var newsletter = await _newsletterService.GetNewsletterByKeyAsync(newsletterKey);

            if (newsletter != null) {
                TelegramChat!.State = SendingNewsletterState;
                TelegramChat["newsletter"] = newsletterKey;
                await ReplyTextMessageAsync("Ok, now send me the text formatted as HTML");

                await TelegramContext.SaveChangesAsync();
            }
            else {
                await ReplyTextMessageAsync(
                    $"The newsletter {newsletterKey} doesn't exist.\n" +
                    $"Use /create_newsletter {newsletterKey} - to create it");
            }
        }
        
        [ChatRoleFilter(ChatRole.Administrator, ChatRole.Moderator), CommandFilter("send_global_newsletter")]
        public async Task SendGlobalNewsletter() {
            if (MessageCommand.Parameters.Count != 0) {
                await ReplyTextMessageAsync("Usage:\n/send_global_newsletter");
                return;
            }
            
            TelegramChat!.State = SendingNewsletterState;
            TelegramChat["newsletter"] = null;
            await ReplyTextMessageAsync("Ok, now send me the text formatted as HTML");

            await TelegramContext.SaveChangesAsync();
        }
        
        [ChatRoleFilter(ChatRole.Administrator, ChatRole.Moderator), ChatStateFilter(SendingNewsletterState), 
         UpdateTypeFilter(UpdateType.Message), NoCommandFilter]
        public async Task SendNewsletterGetText() {
            var newsletterKey = TelegramChat!["newsletter"];
            
            Newsletter? newsletter = null;
            if (newsletterKey is not null) {
                newsletter = await _newsletterService.GetNewsletterByKeyAsync(newsletterKey);
            }

            Func<TelegramChat, Task> sendFunction;
            switch (Update.Message?.Type) {
                case MessageType.Text:
                    sendFunction = async chat => await BotData.Bot.SendTextMessageAsync(chat.Id, Update.Message.Text!, ParseMode.Html);
                    break;
                case MessageType.Photo:
                    sendFunction = async chat => await BotData.Bot.SendPhotoAsync(chat.Id, 
                        Update.Message!.Photo!.First().FileId, 
                        Update.Message.Text);
                    break;
                case MessageType.Audio:
                    sendFunction = async chat => await BotData.Bot.SendAudioAsync(chat.Id,
                        Update.Message!.Audio!.FileId,
                        Update.Message.Text);
                    break;
                case MessageType.Sticker:
                    sendFunction = async chat => await BotData.Bot.SendStickerAsync(chat.Id,
                        Update.Message!.Sticker!.FileId);
                    break;
                // TODO Implements poll?
                default:
                    await ReplyTextMessageAsync("Only text, photo, audio or sticker is supported as a type of " +
                                                "message, please send one of these type");
                    return;
            }

            if (newsletter != null) {
                await ReplyTextMessageAsync("Ok, sending the newsletter...");
                var result = await _newsletterService.SendNewsletterAsync(newsletterKey!, sendFunction);
                await ReplyTextMessageAsync("Finished - report:\n" +
                                            $"Successful delivery: {result.TotalSuccesses}\n" +
                                            $"Failed delivery: {result.TotalErrors}");
                TelegramChat.State = null;
                await TelegramContext.SaveChangesAsync();
            }
            else if (newsletterKey == null) {
                await ReplyTextMessageAsync("Ok, sending the global newsletter...");
                var result = await _newsletterService.SendNewsletterAsync(sendFunction);
                await ReplyTextMessageAsync("Finished - report:\n" +
                                            $"Successful delivery: {result.TotalSuccesses}\n" +
                                            $"Failed delivery: {result.TotalErrors}");
                TelegramChat.State = null;
                await TelegramContext.SaveChangesAsync();
            }
        }
        
        

        private async Task<bool> CheckIfAdminInGroups() {
            if (TelegramChat is {Type: ChatType.Supergroup or ChatType.Group}) {
                var administrators = (await BotData.Bot.GetChatAdministratorsAsync(TelegramChat.Id))
                    .Select(a => a.User.Id);

                if (Update.Message?.From is null || !administrators.Contains(Update.Message.From.Id)) {
                    // Do not answer, just to avoid bot spam
                    return false;
                }
            }

            return true;
        }
    }

    public static class NewsletterControllerExtensions {
        public static IDispatcherBuilder RegisterNewsletterController<TContext>(
            this IDispatcherBuilder dispatcherBuilder) where TContext : TelegramContext {

            dispatcherBuilder.AddControllers(typeof(NewsletterController<TContext>));

            return dispatcherBuilder;
        }
    }
}