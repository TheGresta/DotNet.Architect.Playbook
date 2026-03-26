namespace Playbook.Messaging.MassTransit.Infrastructure.Messaging;

public record MessageBusOptions
{
    public const string SectionName = "MessageBus";
    public string Host { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
