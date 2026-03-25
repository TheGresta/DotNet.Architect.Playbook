namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

public sealed record MessageEndpointDefinition
{
    public string ExchangeName { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public TimeSpan? Ttl { get; set; }
    public bool WaitForConfirm { get; set; } = true;
    public bool AutoCreate { get; set; } = true;
    public string? DeadLetterExchange { get; set; }
    public string? DeadLetterRoutingKey { get; set; }
}
