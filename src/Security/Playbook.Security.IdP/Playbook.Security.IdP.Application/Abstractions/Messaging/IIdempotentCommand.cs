namespace Playbook.Security.IdP.Application.Abstractions.Messaging;

/// <summary>
/// Version for commands that return Success/Error only.
/// </summary>
public interface IIdempotentCommand : ICommand
{
    Guid RequestId { get; }
}

/// <summary>
/// Ensures that a command is only processed once, even if sent multiple times.
/// </summary>
/// <typeparam name="TResponse">The expected response type.</typeparam>
public interface IIdempotentCommand<TResponse> : ICommand<TResponse>
{
    /// <summary>
    /// A unique identifier for the specific request instance, provided by the client.
    /// </summary>
    Guid RequestId { get; }
}
