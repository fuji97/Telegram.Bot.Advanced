using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Internal;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.DispatcherFilters;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Advanced.Test;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Advanced.Test
{
    
    class Controller
    {
        [CommandFilter("add"), ChatTypeFilter(ChatType.Private), MessageTypeFilter(MessageType.Text)]
        public static void Add(Update update, MasterContext context, TelegramChat chat, MessageCommand command) {
            Console.WriteLine("Ricevuto comando /add");
            if (command.Parameters.Count < 1) {
                Program.Bot.SendTextMessageAsync(chat.Id, "Ok, inviami il nome che vuoi usare");
                chat.State = (int) ConversationState.Nome;
            }
            else {
                Program.Bot.SendTextMessageAsync(chat.Id, "Ok, inviami il friend code in formato XXXXXXXXX");
                chat.State = (int)ConversationState.FriendCode;
                chat["nome"] = command.Parameters.Join(" ");
            }
            chat.Update(context);
        }

        [ChatStateFilter((int) ConversationState.Nome), NoCommandFilter, MessageTypeFilter(MessageType.Text)]
        public static void GetNome(Update update, MasterContext context, TelegramChat chat) {
            Console.WriteLine($"Nome ricevuto da @{chat?.Username}: {update.Message.Text}");
            if (chat != null) {
                Program.Bot.SendTextMessageAsync(chat.Id, "Ok, adesso inviami il friend code in formato XXXXXXXXX");
                chat["nome"] = update.Message.Text;
                chat.State = (int)ConversationState.FriendCode;
                chat.Update(context);
            }
        }

        [ChatStateFilter((int) ConversationState.FriendCode), NoCommandFilter, MessageTypeFilter(MessageType.Text)]
        public static void GetFriendCode(Update update, MasterContext context, TelegramChat chat) {
            Console.WriteLine($"Ricevuto friend code: {update.Message.Text}");
            if (Regex.IsMatch(update.Message.Text, @"^\d{5}")) {
                Program.Bot.SendTextMessageAsync(
                    chat.Id,
                    $"E' stato impostato come friend code: '{update.Message.Text}'. Sarà possibile cambiarlo in seguito.\n" +
                    $"Ora inviami il server di appartenenza del Master, 'JP' o 'US' (puoi usare la tastiera automatica)",
                    replyMarkup: new ReplyKeyboardMarkup() {
                        Keyboard = new[] {
                            new[] {new KeyboardButton ("JP")},
                            new[] {new KeyboardButton ("US")}
                        }
                    });
                chat["friend_code"] = update.Message.Text;
                chat.State = (int) ConversationState.Server;
                chat.Update(context);
            }
            else
            {
                Program.Bot.SendTextMessageAsync(
                    chat.Id,
                    "Il friend code non è valido, deve avere il seguente formato XXXXXXXXX");
            }
        }

        [ChatStateFilter((int)ConversationState.Server), NoCommandFilter, MessageTypeFilter(MessageType.Text)]
        public static void GetServer(Update update, MasterContext context, TelegramChat chat)
        {
            Console.WriteLine($"Ricevuto server {update.Message.Text}");
            switch (update.Message.Text) {
                case "JP":
                    Program.Bot.SendTextMessageAsync(chat.Id, "Server giapponese impostato, lo screen dei tuoi support o /skip se vuoi saltare questa fase ",
                        replyMarkup: new ReplyKeyboardRemove());
                    chat["server"] = ((int) MasterServer.JP).ToString();
                    chat.State = (int) ConversationState.SupportList;
                    break;
                case "US":
                    Program.Bot.SendTextMessageAsync(chat.Id, "Server americano impostato, lo screen dei tuoi support o /skip se vuoi saltare questa fase ",
                        replyMarkup: new ReplyKeyboardRemove());
                    chat["server"] = ((int)MasterServer.US).ToString();
                    chat.State = (int)ConversationState.SupportList;
                    break;
                default:
                    Program.Bot.SendTextMessageAsync(chat.Id, "Server non valido, specificare 'JP' o 'US'");
                    break;
            }
            chat.Update(context);
        }


        [ChatStateFilter((int) ConversationState.SupportList), NoCommandFilter, MessageTypeFilter(MessageType.Photo)]
        public static void SupportList(Update update, MasterContext context, TelegramChat chat, MessageCommand command)
        {
            Console.WriteLine("Ricevuta foto");
            Program.Bot.SendTextMessageAsync(chat.Id, $"Ok, ora inviami lo screen della lista dei tuoi servant o /skip se vuoi saltare questa fase");
            chat["support_photo"] = update.Message.Photo[0].FileId;
            chat.State = (int) ConversationState.ServantList;
            chat.Update(context);
        }

        [ChatStateFilter((int)ConversationState.ServantList), NoCommandFilter, MessageTypeFilter(MessageType.Photo)]
        public static void ServantList(Update update, MasterContext context, TelegramChat chat, MessageCommand command)
        {
            Console.WriteLine("Ricevuta foto, creazione Master e inserimento");
            Program.Bot.SendTextMessageAsync(chat.Id, $"Ok, Master creato");
            var master = new Master(chat, chat["nome"], chat["friend_code"], (MasterServer) Int32.Parse(chat["server"]), chat["support_photo"],
                update.Message.Photo[0].FileId);
            context.Add(master);

            chat.State = (int)ConversationState.Idle;
            chat.Data.Clear();
            chat.Update(context);
        }
    }

    public enum ConversationState {
        Idle = 0,
        Nome = 1,
        FriendCode = 2,
        Server = 3,
        SupportList = 4,
        ServantList = 5
    }
}
