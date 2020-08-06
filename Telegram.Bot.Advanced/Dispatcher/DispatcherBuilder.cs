using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.Dispatcher {
    /// <summary>
    /// Builder for Dispatcher
    /// </summary>
    /// <typeparam name="TContext">Context used by the application</typeparam>
    /// <typeparam name="TController">Controller to use as source of methods</typeparam>
    /// <see cref="Dispatcher{TContext,TController}"/>
    public class DispatcherBuilder<TContext, TController> : IDispatcherBuilder
        where TContext : TelegramContext
        where TController : class, ITelegramController<TContext> {

        private ITelegramBotData _botData;
        private ILogger<Dispatcher<TContext, TController>> _logger;
        private List<Type> _controllers;

        /// <inheritdoc />
        public IDispatcherBuilder SetTelegramBotData(ITelegramBotData botData) {
            _botData = botData;
            return this;
        }

        /// <summary>
        /// Set a logger to use, if Dependency Injection is in use, do not call this method
        /// </summary>
        /// <param name="logger">Logger to use</param>
        /// <returns></returns>
        public IDispatcherBuilder SetLogger(ILogger<Dispatcher<TContext, TController>> logger) {
            _logger = logger;
            return this;
        }

        
        public IDispatcherBuilder AddControllers(params Type[] controllers) {
            _controllers ??= new List<Type>();
            _controllers.AddRange(controllers);

            return this;
        }

        /// <inheritdoc />
        public IDispatcher Build() {
            return new Dispatcher<TContext,TController>(_botData, _logger, _controllers);
        }
    }
}