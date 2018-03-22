using System;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var dispatcher = new Dispatcher(typeof(Controller));

            Update testUpdate = new Update() {Id = 10, Message = new Message() {Text = "Ciao"}};

            dispatcher.DispatchUpdate(testUpdate);

            Console.ReadKey();
        }
    }
}
