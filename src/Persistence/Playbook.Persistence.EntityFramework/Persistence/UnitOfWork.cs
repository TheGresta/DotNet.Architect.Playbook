using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Playbook.Persistence.EntityFramework.Application;
using Playbook.Persistence.EntityFramework.Application.Repositories;
using Playbook.Persistence.EntityFramework.Persistence.Context;
using Playbook.Persistence.EntityFramework.Persistence.Repositories;

namespace Playbook.Persistence.EntityFramework.Persistence;

/// <summary>
/// Implements the <see cref="IUnitOfWork"/> interface to coordinate database operations 
/// and manage repository lifetimes within a single business transaction.
/// </summary>
/// <param name="context">The <see cref="ApplicationDbContext"/> used for data persistence.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve repositories from the DI container.</param>
/// <remarks>
/// This class is <see langword="sealed"/> to prevent further inheritance and implements 
/// both <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> for clean resource management.
/// </remarks>
internal sealed class UnitOfWork(
    ApplicationDbContext context,
    IServiceProvider serviceProvider) : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private readonly Dictionary<Type, object> _repositories = [];
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    #region Repositories

    /// <summary>
    /// Gets the user repository.
    /// </summary>
    /// <inheritdoc cref="IUnitOfWork.UserRepository"/>
    public IUserRepository UserRepository => Repository<UserRepository>();

    /// <summary>
    /// Resolves and caches a repository instance.
    /// </summary>
    /// <typeparam name="TRepository">The type of the repository to resolve.</typeparam>
    /// <returns>An instance of <typeparamref name="TRepository"/>.</returns>
    /// <remarks>
    /// <para>
    /// <b>Logic:</b>
    /// 1. Checks the internal cache (<c>_repositories</c>) to see if the repository was already instantiated.<br/>
    /// 2. Attempts to resolve the repository from the <see cref="IServiceProvider"/>.<br/>
    /// 3. If not registered in DI, it falls back to <see cref="Activator.CreateInstance(Type, object[])"/>, 
    /// passing the current <see cref="ApplicationDbContext"/> to the constructor.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if a repository instance cannot be created.</exception>
    private TRepository Repository<TRepository>() where TRepository : class
    {
        var type = typeof(TRepository);

        if (!_repositories.ContainsKey(type))
        {
            var service = serviceProvider.GetService<TRepository>();

            if (service != null)
            {
                _repositories[type] = service;
            }
            else
            {
                var instance = Activator.CreateInstance(type, context)
                    ?? throw new InvalidOperationException($"Could not create instance of {type.Name}");

                _repositories[type] = instance;
            }
        }

        return (TRepository)_repositories[type];
    }

    #endregion

    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);

    #region Transaction Management

    /// <inheritdoc/>
    public bool HasActiveTransaction => _currentTransaction != null;

    /// <summary>
    /// Asynchronously starts a new database transaction if one is not already active.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe.</param>
    public async Task BeginTransactionAsync(CancellationToken ct)
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await context.Database.BeginTransactionAsync(ct);
    }

    /// <summary>
    /// Asynchronously saves changes, commits the current transaction, and cleans up transaction resources.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe.</param>
    /// <remarks>
    /// If an error occurs during save or commit, the transaction is automatically rolled back 
    /// before the exception is re-thrown.
    /// </remarks>
    public async Task CommitTransactionAsync(CancellationToken ct)
    {
        try
        {
            await SaveChangesAsync(ct);
            if (_currentTransaction != null)
                await _currentTransaction.CommitAsync(ct);
        }
        catch
        {
            await RollbackTransactionAsync(ct);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Asynchronously rolls back the current transaction and discards pending changes.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe.</param>
    public async Task RollbackTransactionAsync(CancellationToken ct)
    {
        if (_currentTransaction == null) return;

        await _currentTransaction.RollbackAsync(ct);
        await DisposeTransactionAsync();
    }

    /// <summary>
    /// Disposes the current transaction and resets the transaction state.
    /// </summary>
    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    #endregion

    /// <inheritdoc/>
    public void ClearChangeTracker() => context.ChangeTracker.Clear();

    /// <inheritdoc/>
    public void SetCommandTimeout(int seconds) => context.Database.SetCommandTimeout(seconds);

    #region Disposal

    /// <summary>
    /// Performs synchronous disposal of the database context and active transactions.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _currentTransaction?.Dispose();
        context.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs asynchronous disposal of the database context and active transactions.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (_currentTransaction != null) await _currentTransaction.DisposeAsync();
        await context.DisposeAsync();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}