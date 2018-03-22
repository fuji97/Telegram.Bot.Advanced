using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Advanced.DispatcherFilters;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Test
{
    
    class Controller
    {
        [IdFilter(10)]
        public static void Test1(Update update) {
            Console.WriteLine("Test 10");
        }

        [IdFilter(10), TextMessage("Ciao")]
        public static void Test12(Update update)
        {
            Console.WriteLine("Test 10 con Ciao");
        }

        [IdFilter(10), TextMessage("Mondo")]
        public static void Test123(Update update)
        {
            Console.WriteLine("Test 10 con Mondo");
        }

        [IdFilter(20)]
        public static void Test2(Update update) {
            Console.WriteLine("Test 20");
        }
    }
}
