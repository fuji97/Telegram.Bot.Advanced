using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.DispatcherFilters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class IdFilterAttribute : DispatcherFilterAttribute {
        private readonly int _id = 0;

        public IdFilterAttribute(int id) {
            _id = id;
        }

        public override bool IsValid(Update update) {
            return update.Id == _id;
        }
    }
}
