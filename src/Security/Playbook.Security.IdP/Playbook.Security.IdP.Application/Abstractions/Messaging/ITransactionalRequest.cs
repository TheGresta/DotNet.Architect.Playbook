namespace Playbook.Security.IdP.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface to indicate that the request must be executed within a database transaction.
/// If the handler fails, all changes are rolled back.
/// </summary>
public interface ITransactionalRequest
{
}

