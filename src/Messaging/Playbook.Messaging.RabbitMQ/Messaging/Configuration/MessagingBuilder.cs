using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Engine.Consumer;
using Playbook.Messaging.RabbitMQ.Messaging.Engine.Producer;
using Playbook.Messaging.RabbitMQ.Messaging.Internal;

namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

/// <summary>
/// Provides a fluent API for configuring message producers and consumers within the RabbitMQ messaging ecosystem.
/// This builder coordinates the registration of message-specific metadata and the corresponding 
/// infrastructure required to handle them.
/// </summary>
/// <remarks>
/// Initialized during the <c>AddRabbitMessaging</c> extension call, this class acts as a bridge between 
/// the user-defined message contracts and the internal RabbitMQ implementation.
/// </remarks>
public sealed class MessagingBuilder(
    IServiceCollection services,
    ConsumerRegistry consumerRegistry,
    MessageEndpointRegistry endpointRegistry)
{
    /// <summary>
    /// Registers a producer for a specific message type <typeparamref name="T"/>.
    /// Configures the exchange topology and adds the <see cref="IProducer{T}"/> to the service container.
    /// </summary>
    /// <typeparam name="T">The message contract type to be published.</typeparam>
    /// <param name="configure">A delegate to configure the <see cref="MessageTypeBuilder{T}"/> for this specific message.</param>
    /// <returns>The current <see cref="MessagingBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// The producer is registered as a <see cref="ServiceDescriptor.Singleton"/> to maximize performance 
    /// and leverage internal connection and channel pooling.
    /// </remarks>
    public MessagingBuilder AddProducer<T>(Action<MessageTypeBuilder<T>> configure) where T : class
    {
        var builder = new MessageTypeBuilder<T>();
        configure(builder);

        // Store the metadata required for the producer to identify its target exchange and routing logic
        endpointRegistry.AddDefinition<T>(builder.Build());

        // Register the typed producer implementation. Singleton lifetime is safe due to the thread-safe 
        // nature of the internal RabbitProducer.
        services.AddSingleton<IProducer<T>, RabbitProducer<T>>();

        return this;
    }

    /// <summary>
    /// Registers a consumer and its associated handlers for a specific message type <typeparamref name="T"/>.
    /// Automatically instantiates a dedicated <see cref="RabbitConsumerEngine{T}"/> as a hosted service.
    /// </summary>
    /// <typeparam name="T">The message contract type to be consumed.</typeparam>
    /// <param name="configure">A delegate to configure the <see cref="ConsumerRegistrationBuilder{T}"/>.</param>
    /// <returns>The current <see cref="MessagingBuilder"/> instance for method chaining.</returns>
    /// <remarks>
    /// Each consumer type receives its own background worker engine, ensuring isolation and allowing 
    /// for independent scaling and concurrency control per message type.
    /// </remarks>
    public MessagingBuilder AddConsumer<T>(Action<ConsumerRegistrationBuilder<T>> configure) where T : class
    {
        var regBuilder = new ConsumerRegistrationBuilder<T>(services, consumerRegistry);
        configure(regBuilder);

        // Register a dedicated background service instance for this specific message type.
        // This engine will manage the worker pool and RabbitMQ consumer loop.
        services.AddHostedService<RabbitConsumerEngine<T>>();

        return this;
    }
}
