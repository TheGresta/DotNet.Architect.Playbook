using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Internal;

namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

public sealed class ConsumerRegistrationBuilder<T>(
    IServiceCollection services,
    ConsumerRegistry registry) where T : class
{
    public ConsumerRegistrationBuilder<T> AddHandler<THandler>()
        where THandler : class, IIntegrationEventHandler<T>
    {
        // Register the handler type so DI can find it later
        services.AddScoped<THandler>();

        // Directly update the registry instance we passed in
        registry.RegisterHandler<T, THandler>();

        return this;
    }
}
