using Microsoft.Extensions.DependencyInjection.Extensions;

using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Internal;

using RabbitMQ.Client;

namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

/// <summary>
/// Provides extension methods for the <see cref="IServiceCollection"/> to facilitate the registration 
/// of the RabbitMQ messaging infrastructure. This acts as the primary entry point for configuring 
/// connections, topology management, and message dispatching.
/// </summary>
public static class MessagingExtensions
{
    /// <summary>
    /// Configures and adds RabbitMQ messaging services to the dependency injection container.
    /// Sets up the core infrastructure, including connection factories, persistent connections, 
    /// and internal registries.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">A delegate to configure the <see cref="RabbitOptions"/>.</param>
    /// <returns>A <see cref="MessagingBuilder"/> to allow for fluent configuration of producers and consumers.</returns>
    /// <remarks>
    /// This method utilizes RabbitMQ.Client v7+ optimizations, including built-in automatic recovery 
    /// and asynchronous connection handling. It also ensures that critical messaging components 
    /// are registered as singletons to maintain state across the application lifetime.
    /// </remarks>
    public static MessagingBuilder AddRabbitMessaging(this IServiceCollection services, Action<RabbitOptions> configure)
    {
        // 1. Initialize the options class with default values
        var options = new RabbitOptions();

        // 2. Execute the configuration delegate to apply user-defined settings or environment bindings
        configure(options);

        // 3. Register options as Singleton so the internal engines can access shared configuration
        services.AddSingleton(options);

        var endpointRegistry = new MessageEndpointRegistry();
        var consumerRegistry = new ConsumerRegistry();

        // 4. Core Infrastructure Registration: Ensuring idempotent registration of internal services
        services.TryAddSingleton(endpointRegistry);
        services.TryAddSingleton(consumerRegistry);
        services.TryAddSingleton<PersistentConnection>();
        services.TryAddSingleton<ITopologyManager, RabbitTopologyManager>();
        services.TryAddSingleton<IMessageDispatcher, MessageDispatcher>();

        // 5. RabbitMQ Client Factory Setup (v7+ Optimized):
        // Configures the low-level connection factory with high-availability parameters.
        services.TryAddSingleton<IConnectionFactory>(sp =>
        {
            // Resolve the latest options from the service provider
            var opt = sp.GetRequiredService<RabbitOptions>();

            return new ConnectionFactory
            {
                HostName = opt.HostName,
                UserName = opt.UserName,
                Password = opt.Password,
                Port = opt.Port,
                VirtualHost = opt.VirtualHost,

                // Reliability Engineering: Automatic recovery is natively handled in v7,
                // but we explicitly tune the recovery interval for faster failover.
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };
        });

        return new MessagingBuilder(services, consumerRegistry, endpointRegistry);
    }
}
