using Microsoft.Extensions.DependencyInjection.Extensions;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Internal;

namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

/// <summary>
/// Provides a specialized builder for registering message handlers and configuring 
/// consumer-specific behavior for the message type <typeparamref name="T"/>.
/// This builder facilitates the link between incoming RabbitMQ messages and the 
/// application-level logic defined in <see cref="IIntegrationEventHandler{T}"/>.
/// </summary>
/// <typeparam name="T">The message contract type to be handled.</typeparam>
/// <remarks>
/// Handlers registered through this builder are automatically added to the 
/// <see cref="IServiceCollection"/> with a <see cref="ServiceLifetime.Scoped"/> lifetime, 
/// ensuring that dependencies (like database contexts) are correctly isolated per message dispatch.
/// </remarks>
public sealed class ConsumerRegistrationBuilder<T>(
    IServiceCollection services,
    ConsumerRegistry registry) where T : class
{
    /// <summary>
    /// Registers a specific handler implementation for the message type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="THandler">
    /// The implementation type of <see cref="IIntegrationEventHandler{T}"/> that will process the message.
    /// </typeparam>
    /// <returns>The current <see cref="ConsumerRegistrationBuilder{T}"/> instance for method chaining.</returns>
    /// <remarks>
    /// The handler is registered as Scoped because the <see cref="MessageDispatcher"/> 
    /// creates a new asynchronous service scope for every individual message processed.
    /// </remarks>
    public ConsumerRegistrationBuilder<T> AddHandler<THandler>()
        where THandler : class, IIntegrationEventHandler<T>
    {
        // Register the handler in the DI container. 
        // Scoped lifetime ensures that each message has its own instance of the handler and its dependencies.
        services.TryAddScoped<THandler>();

        // Update the internal registry so the dispatcher knows which types to resolve when a message arrives.
        registry.RegisterHandler<T, THandler>();

        return this;
    }
}
