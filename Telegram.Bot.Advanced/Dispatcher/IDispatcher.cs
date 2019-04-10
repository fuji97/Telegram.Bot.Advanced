using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Dispatcher {
    /// <summary>
    /// Interface for the Dispatcher class
    /// </summary>
    public interface IDispatcher {
        /// <summary>
        /// Returns the Type of the contoller in use by the dispatcher
        /// </summary>
        /// <returns>Type of the controller in use</returns>
        Type GetControllerType();
        
        /// <summary>
        /// Returns the context in use by the application
        /// </summary>
        /// <returns>Context in use by the application</returns>
        Type GetContextType();
        
        /// <summary>
        /// Dispatch the update
        /// </summary>
        /// <param name="update">Update to dispatch</param>
        /// <param name="provider">Provider used to get dependencies</param>
        /// <returns></returns>
        Task DispatchUpdateAsync(Update update, IServiceProvider provider);
        
        /// <summary>
        /// Add the controller as dependency in Dependency Injection
        /// </summary>
        /// <param name="services">The service collection used to add the dependencies</param>
        void RegisterController(IServiceCollection services);
        
        /// <summary>
        /// Set the provider used to get dependencies
        /// </summary>
        /// <param name="provider">Provider used to get dependencies</param>
        void SetServices(IServiceProvider provider);
    }
}