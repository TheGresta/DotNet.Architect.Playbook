namespace Playbook.Messaging.MassTransit.Saga.Infrastructure.Messaging;

/// <summary>
/// Represents the configuration options required to establish a connection with the message broker.
/// This record is typically bound to the "MessageBus" section of the application configuration.
/// </summary>
public record MessageBusOptions
{
    /// <summary>
    /// The constant section name used for configuration binding (e.g., in appsettings.json).
    /// </summary>
    public const string SectionName = "MessageBus";

    /// <summary>
    /// Gets or sets the host address of the message broker (e.g., "rabbitmq://localhost" or "localhost").
    /// </summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the username used for authenticating with the message broker.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the password used for authenticating with the message broker.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
