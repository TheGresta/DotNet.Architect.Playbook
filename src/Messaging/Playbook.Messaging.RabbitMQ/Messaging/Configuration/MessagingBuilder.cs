using Playbook.Messaging.RabbitMQ.Messaging.Abstractions;
using Playbook.Messaging.RabbitMQ.Messaging.Engine.Consumer;
using Playbook.Messaging.RabbitMQ.Messaging.Engine.Producer;
using Playbook.Messaging.RabbitMQ.Messaging.Internal;

namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

public sealed class MessagingBuilder(IServiceCollection services, ConsumerRegistry consumerRegistry, MessageEndpointRegistry endpointRegistry)
{
    public MessagingBuilder AddProducer<T>(Action<MessageTypeBuilder<T>> configure) where T : class
    {
        var builder = new MessageTypeBuilder<T>();
        configure(builder);

        endpointRegistry.AddDefinition<T>(builder.Build());
        services.AddSingleton<IProducer<T>, RabbitProducer<T>>();
        return this;
    }

    public MessagingBuilder AddConsumer<T>(Action<ConsumerRegistrationBuilder<T>> configure) where T : class
    {
        var regBuilder = new ConsumerRegistrationBuilder<T>(services, consumerRegistry);
        configure(regBuilder);

        services.AddHostedService<RabbitConsumerEngine<T>>();
        return this;
    }
}
