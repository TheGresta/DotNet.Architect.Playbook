using ErrorOr;

using MediatR;

namespace Playbook.Security.IdP.Application.Abstractions.Messaging;

/// <summary>
/// Represents a command that returns a specific value.
/// Use this for operations like 'RegisterUser' where you need the new ID back.
/// </summary>
public interface ICommand<TResponse> : IRequest<ErrorOr<TResponse>>
{
}

/// <summary>
/// Represents a command that returns a simple Success/Error status.
/// Use this for operations like 'RevokeConsent' or 'UpdatePassword'.
/// </summary>
public interface ICommand : IRequest<ErrorOr<Success>>
{
}
