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
        // Register as Scoped: Dispatcher creates a scope per message
        services.AddScoped<THandler>();
        registry.RegisterHandler<T, THandler>();
        return this;
    }
}
