using Playbook.Security.IdP.Domain.Repositories;

namespace Playbook.Security.IdP.Domain.Common;

/// <summary>
/// Defines the contract for a Unit of Work, coordinating the writing out of changes 
/// and the management of database transactions across multiple repositories.
/// </summary>
/// <remarks>
/// This pattern ensures that all operations within a single scope either succeed or fail as a single unit, 
/// maintaining the integrity of the underlying data store.
/// </remarks>
public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }
    IAuthenticationSessionRepository AuthenticationSessionRepository { get; }
    IQrChallengeRepository QrChallengeRepository { get; }
    IUserConsentRepository UserConsentRepository { get; }
    IUserDeviceRepository UserDeviceRepository { get; }
    IPermissionRepository PermissionRepository { get; }
    IRoleRepository RoleRepository { get; }

    /// <summary>
    /// Asynchronously persists all changes made in this unit of work to the underlying database.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while waiting for the task to complete. 
    /// The default is <see langword="default"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the 
    /// number of state entries written to the database.
    /// </returns>
    /// <exception cref="DbUpdateException">Thrown if an error occurs while saving to the database.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Thrown if a concurrency violation is encountered.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously starts a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe. The default is <see langword="default"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous initialization of the transaction.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a transaction is already in progress.</exception>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously commits the current database transaction, applying all pending changes permanently.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe. The default is <see langword="default"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous commit operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there is no active transaction to commit.</exception>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously discards all changes made within the scope of the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe. The default is <see langword="default"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous rollback operation.</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether a transaction is currently active and uncommitted.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the <see cref="IUnitOfWork"/> has an ongoing transaction; otherwise, <see langword="false"/>.
    /// </value>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Clears the change tracker, detaching all currently tracked entities from the context.
    /// </summary>
    /// <remarks>
    /// This is typically used in bulk operations or to recover from a failed save state without 
    /// disposing of the entire unit of work instance.
    /// </remarks>
    void ClearChangeTracker();

    /// <summary>
    /// Sets the timeout period, in seconds, for commands executed within this unit of work.
    /// </summary>
    /// <param name="seconds">The number of seconds to wait before a command times out.</param>
    void SetCommandTimeout(int seconds);
}
