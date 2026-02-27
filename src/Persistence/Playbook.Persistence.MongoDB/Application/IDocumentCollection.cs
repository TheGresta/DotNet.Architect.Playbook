using Playbook.Persistence.MongoDB.Application.Repositories;

namespace Playbook.Persistence.MongoDB.Application;

/// <summary>
/// Defines the contract for a Unit of Work that orchestrates the storage and retrieval of MongoDB documents.
/// </summary>
/// <remarks>
/// This interface manages the lifecycle of multiple repositories and ensures that data integrity 
/// is maintained through coordinated transaction management.
/// </remarks>
public interface IDocumentCollection : IDisposable
{
    /// <summary>
    /// Gets the repository responsible for managing <see cref="ExceptionMessageDocument"/> entities.
    /// </summary>
    /// <value>An instance of <see cref="IExceptionMessageDocumentRepository"/>.</value>
    IExceptionMessageDocumentRepository ExceptionMessageDocuments { get; }

    // Add other repos here...

    /// <summary>
    /// Starts a new multi-document transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete. The default is <see langword="default"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// MongoDB transactions require a replica set or a sharded cluster. Ensure the underlying client 
    /// supports sessions before invoking this method.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if a transaction is already in progress.</exception>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current multi-document transaction asynchronously, persisting all changes made during the session.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete. The default is <see langword="default"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no active transaction is found to commit.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the <paramref name="cancellationToken"/> is canceled.</exception>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts the current multi-document transaction asynchronously and rolls back any pending changes.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete. The default is <see langword="default"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method should typically be called within a <c>catch</c> block to ensure data consistency 
    /// when an error occurs during the unit of work.
    /// </remarks>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
