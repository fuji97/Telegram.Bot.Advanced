using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Telegram.Bot.Advanced.DispatcherFilters;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced
{
    public class Dispatcher {
        private readonly IEnumerable<MethodInfo> _methods;

        public Dispatcher(Type controller) {
            _methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                    .Where(m => m.GetCustomAttributes(typeof(DispatcherFilterAttribute), false).Length > 0);
        }

        public void DispatchUpdate(Update update) {
            foreach (var method in _methods.Where(m => m.GetCustomAttributes()
                                                        .Where(att => att is DispatcherFilterAttribute)
                                                        .All(attr => ((DispatcherFilterAttribute) attr).IsValid(update)))) {
                var parameters = method.GetParameters();
                if (!parameters.Any() || parameters[0].ParameterType != typeof(Update)) {
                    throw new InvalidRouteMethodArguments(parameters?[0], "The first parameter must be the Update");
                }
                var arguments = new List<Object> {update};
                foreach (var par in parameters.Skip(1)) {
                    if (par.ParameterType == typeof(string)) {
                            arguments.Add("parsedCOmmand"); // TODO
                    } else if (par.ParameterType == typeof(IEnumerable<Object>)) {
                        arguments.Add(null);
                    } else {
                        throw new InvalidRouteMethodArguments(par);
                    }
                }
                method.Invoke(null, arguments.ToArray());
            }
        }
    }
}
