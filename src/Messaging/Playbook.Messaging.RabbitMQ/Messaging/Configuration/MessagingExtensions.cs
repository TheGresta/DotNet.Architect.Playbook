using Playbook.Messaging.RabbitMQ.Messaging.Internal;

using RabbitMQ.Client;

namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

public static class MessagingExtensions
{
    public static MessagingBuilder AddRabbitMessaging(this IServiceCollection services, Action<RabbitOptions> configure)
    {
        // 1. Initialize the class with default values
        var options = new RabbitOptions();

        // 2. Execute the configuration delegate (this is where .Bind() or manual sets happen)
        configure(options);

        // 3. Register as Singleton so the rest of the internal engine can inject it
        services.AddSingleton(options);

        var endpointRegistry = new MessageEndpointRegistry();
        var consumerRegistry = new ConsumerRegistry();

        // 4. Core Infrastructure Registration
        services.AddSingleton(endpointRegistry);
        services.AddSingleton(consumerRegistry);
        services.AddSingleton<PersistentConnection>();

        // 5. RabbitMQ Client Factory Setup (v7+ Optimized)
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            // We pull the options from the container to ensure we have the latest configured state
            var opt = sp.GetRequiredService<RabbitOptions>();

            return new ConnectionFactory
            {
                HostName = opt.HostName,
                UserName = opt.UserName,
                Password = opt.Password,
                Port = opt.Port,
                VirtualHost = opt.VirtualHost,
                // Big Tech Optimization: Automatic recovery is built into v7, 
                // but we ensure it's tuned for high availability
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };
        });

        return new MessagingBuilder(services, consumerRegistry, endpointRegistry);
    }
}
