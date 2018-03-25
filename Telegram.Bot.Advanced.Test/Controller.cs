using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Advanced.DispatcherFilters;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Test
{
    
    class Controller
    {
        [CommandFilter("saluto"), ChatTypeFilter(ChatType.Private)]
        public static void Test1(Update update, TelegramChat chat, MessageCommand command) {
            Console.WriteLine("Ricevuto comando /saluto");
            chat.State = (int) ConversationState.Nome;
            if (command.Parameters.Count > 0) {
                chat.AddData("saluto", command.Parameters.First());
            }
            chat.Update();
        }

        [ChatStateFilter((int) ConversationState.Nome), NoCommandFilter]
        public static void GetName(Update update, TelegramChat chat) {
            Console.WriteLine("Ricevuto un nome");
            chat.State = (int)ConversationState.Cognome;
            chat.AddData("nome", update.Message.Text);
            chat.Update();
        }

        [ChatStateFilter((int)ConversationState.Cognome), NoCommandFilter]
        public static void GetCognome(Update update, TelegramChat chat) {
            Console.WriteLine("Ricevuto un cognome");
            chat.State = (int)ConversationState.Idle;
            chat.AddData("cognome", update.Message.Text);
            chat.Update();
        }


        [CommandFilter("fai"), ChatTypeFilter(ChatType.Private)]
        public static void Fai(Update update, TelegramChat chat, MessageCommand command)
        {
            Console.WriteLine("Ricevuto comando /fai");
            chat.State = (int)ConversationState.Nome;
            var saluto = chat.Data.FirstOrDefault(d => d.Key == "saluto");
            var nome = chat.Data.FirstOrDefault(d => d.Key == "nome");
            var cognome = chat.Data.FirstOrDefault(d => d.Key == "cognome");
            Program.RealBot.SendTextMessageAsync(chat.Id, $"{saluto}! {nome} {cognome}");
        }
    }

    public enum ConversationState {
        Idle = 0,
        Nome = 1,
        Cognome = 2
    }
}
