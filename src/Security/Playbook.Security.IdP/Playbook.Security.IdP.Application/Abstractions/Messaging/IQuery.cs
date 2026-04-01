using ErrorOr;

using MediatR;

namespace Playbook.Security.IdP.Application.Abstractions.Messaging;

/// <summary>
/// Represents a read-only query. 
/// Always returns an ErrorOr wrapper to handle potential "Not Found" or "Forbidden" states.
/// </summary>
public interface IQuery<TResponse> : IRequest<ErrorOr<TResponse>>
{
}
