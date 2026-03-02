using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Playbook.Persistence.EntityFramework.Application;
using Playbook.Persistence.EntityFramework.Application.Repositories;
using Playbook.Persistence.EntityFramework.Persistence.Context;

namespace Playbook.Persistence.EntityFramework.Persistence;

internal sealed class UnitOfWork(
    ApplicationDbContext context,
    IServiceProvider serviceProvider) : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private readonly Dictionary<Type, object> _cache = [];
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    #region Repositories

    public IUserRepository UserRepository => GetRepository<IUserRepository>();

    private T GetRepository<T>() where T : class
        => (T)(_cache.GetValueOrDefault(typeof(T))
           ?? (_cache[typeof(T)] = serviceProvider.GetRequiredService<T>()));

    #endregion

    public Task<int> SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);

    #region Transaction Management

    public bool HasActiveTransaction => _currentTransaction != null;

    public async Task BeginTransactionAsync(CancellationToken ct)
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await context.Database.BeginTransactionAsync(ct);
    }

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

    public async Task RollbackTransactionAsync(CancellationToken ct)
    {
        if (_currentTransaction == null) return;

        await _currentTransaction.RollbackAsync(ct);
        await DisposeTransactionAsync();
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    #endregion

    public void ClearChangeTracker() => context.ChangeTracker.Clear();

    public void SetCommandTimeout(int seconds) => context.Database.SetCommandTimeout(seconds);

    #region Disposal

    public void Dispose()
    {
        if (_disposed) return;
        _currentTransaction?.Dispose();
        context.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

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