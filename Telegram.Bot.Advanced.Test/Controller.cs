using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Advanced.DispatcherFilters;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Test
{
    
    class Controller
    {
        [CommandFilter("ciao")]
        public static void Test1(Update update) {
            Console.WriteLine("Test 10");
        }

        [NoCommandFilter()]
        public static void Test12(Update update)
        {
            Console.WriteLine("Test 10 con Ciao");
        }

        [NoCommandFilter]
        public static void Test123(Update update)
        {
            Console.WriteLine("Test 10 con Mondo");
        }

        [CommandFilter("help")]
        public static void Test2(Update update) {
            Console.WriteLine("Test 20");
        }
    }
}
