using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;

namespace Telegram.Bot.Advanced.Core.Dispatcher {
    public class DispatcherBuilder<TContext> : IDispatcherBuilder
        where TContext : TelegramContext {

        private ITelegramBotData? _botData;
        private ILogger<Dispatcher<TContext>>? _logger;
        private List<Type> _controllers = new();

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
        public IDispatcherBuilder SetLogger(ILogger<Dispatcher<TContext>> logger) {
            _logger = logger;
            return this;
        }

        
        public IDispatcherBuilder AddControllers(params Type[] controllers) {
            _controllers.AddRange(controllers);

            return this;
        }

        /// <inheritdoc />
        public IDispatcher Build() {
            if (_botData == null) {
                throw new TelegramBotAdvancedException(
                    "Cannot build a Dispatcher without ITelegramBotData. Call SetTelegramBotData() before calling Build().");
            }
            
            return new Dispatcher<TContext>(_botData, _controllers, _logger);
        }
    }

    /// <summary>
    /// Builder for Dispatcher
    /// </summary>
    /// <typeparam name="TContext">Context used by the application</typeparam>
    /// <typeparam name="TController">Controller to use as source of methods</typeparam>
    /// <see cref="Dispatcher{TContext,TController}"/>
    public class DispatcherBuilder<TContext, TController> : IDispatcherBuilder
        where TContext : TelegramContext
        where TController : class, ITelegramController<TContext> {

        private ITelegramBotData? _botData;
        private ILogger<Dispatcher<TContext, TController>>? _logger;
        private List<Type> _controllers = new();

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
        public IDispatcherBuilder SetLogger(ILogger<Dispatcher<TContext, TController>>? logger) {
            _logger = logger;
            return this;
        }

        
        public IDispatcherBuilder AddControllers(params Type[] controllers) {
            _controllers.AddRange(controllers);

            return this;
        }

        /// <inheritdoc />
        public IDispatcher Build() {
            if (_botData == null) {
                throw new NullReferenceException("Bot data not set. Call SetTelegramBotData() before Build()");
            }
            
            return new Dispatcher<TContext,TController>(_botData, _controllers, _logger);
        }
    }
}