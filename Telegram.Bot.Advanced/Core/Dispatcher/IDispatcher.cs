using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Dispatcher;

/// <summary>
/// Interface for the Dispatcher class
/// </summary>
public interface IDispatcher {
    /// <summary>
    /// Returns the Type of the controllers in use by the dispatcher
    /// </summary>
    /// <returns>Types of the controllers in use</returns>
    IList<Type> GetControllersType();

    /// <summary>
    /// Returns the context in use by the application
    /// </summary>
    /// <returns>Context in use by the application</returns>
    Type GetContextType();

    /// <summary>
    /// Dispatch the update
    /// </summary>
    /// <param name="update">Update to dispatch</param>
    /// <param name="provider">Provider used to resolve dependencies for this dispatch. The dispatcher creates its own async scope from it.</param>
    /// <param name="cancellationToken">Token used to cancel the dispatch</param>
    Task DispatchUpdateAsync(Update update, IServiceProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add the controllers as dependencies in Dependency Injection
    /// </summary>
    /// <param name="services">The service collection used to add the dependencies</param>
    void RegisterController(IServiceCollection services);

    /// <summary>
    /// Called when an error is raised by the Telegram API
    /// </summary>
    /// <param name="exception">Raised exception</param>
    /// <param name="provider">Provider used to resolve dependencies for error handling. The dispatcher creates its own async scope from it.</param>
    /// <param name="cancellationToken">Token used to cancel the error handling</param>
    Task HandleErrorAsync(Exception exception, IServiceProvider provider, CancellationToken cancellationToken = default);
}
