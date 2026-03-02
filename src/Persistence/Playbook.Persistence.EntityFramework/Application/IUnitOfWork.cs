using Playbook.Persistence.EntityFramework.Application.Repositories;

namespace Playbook.Persistence.EntityFramework.Application;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    bool HasActiveTransaction { get; }

    void ClearChangeTracker();
    void SetCommandTimeout(int seconds);
}