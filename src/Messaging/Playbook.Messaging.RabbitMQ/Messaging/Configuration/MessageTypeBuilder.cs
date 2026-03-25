namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

public sealed class MessageTypeBuilder<T> where T : class
{
    private readonly MessageEndpointDefinition _definition = new()
    {
        // Default exchange name based on type if not specified
        ExchangeName = typeof(T).Name
    };

    public MessageTypeBuilder<T> ToExchange(string name)
    {
        _definition.ExchangeName = name;
        return this;
    }

    public MessageTypeBuilder<T> WithRoutingKey(string key)
    {
        _definition.RoutingKey = key;
        return this;
    }

    public MessageTypeBuilder<T> WithTtl(TimeSpan ttl)
    {
        _definition.Ttl = ttl;
        return this;
    }

    public MessageTypeBuilder<T> SetFireAndForget()
    {
        _definition.WaitForConfirm = false;
        return this;
    }

    public MessageTypeBuilder<T> WithDeadLetter(string exchangeName, string? routingKey = null)
    {
        _definition.DeadLetterExchange = exchangeName;
        _definition.DeadLetterRoutingKey = routingKey ?? $"{typeof(T).Name}.Error";
        return this;
    }

    internal MessageEndpointDefinition Build() => _definition;
}
