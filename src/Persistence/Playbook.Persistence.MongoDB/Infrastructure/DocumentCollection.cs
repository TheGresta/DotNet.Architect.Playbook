using MongoDB.Driver;

using Playbook.Persistence.MongoDB.Application;
using Playbook.Persistence.MongoDB.Application.Repositories;
using Playbook.Persistence.MongoDB.Infrastructure.Contexts;
using Playbook.Persistence.MongoDB.Infrastructure.Repositories;

namespace Playbook.Persistence.MongoDB.Infrastructure;

internal class DocumentCollection(MongoDbContext context) : IDocumentCollection
{
    private IClientSessionHandle? _session;

    // Backing fields for "Singleton per Scope" repositories
    private IExceptionMessageDocumentRepository? _exceptionRepo;

    public IExceptionMessageDocumentRepository ExceptionMessageDocuments
        => _exceptionRepo ??= new ExceptionMessageDocumentRepository(context);

    // If you have many repos and want to avoid field bloat, 
    // you can use a Dictionary<Type, object>, but for 5-10 repos, fields are faster.

    public async Task BeginTransactionAsync(CancellationToken ct)
    {
        if (_session is { IsInTransaction: true })
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        _session?.Dispose();

        // 1. Start the session
        _session = await context.Client.StartSessionAsync(cancellationToken: ct);

        // 2. Start the transaction
        _session.StartTransaction();

        // 3. Crucial: Tell the context about the session so the BaseRepository can use it
        context.SetSession(_session);
    }

    public async Task CommitTransactionAsync(CancellationToken ct)
    {
        try
        {
            if (_session is { IsInTransaction: true })
            {
                await _session.CommitTransactionAsync(ct);
            }
        }
        catch
        {
            await RollbackTransactionAsync(ct);
            throw;
        }
        finally
        {
            ClearSession();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct)
    {
        if (_session is { IsInTransaction: true })
        {
            await _session.AbortTransactionAsync(ct);
        }
        ClearSession();
    }

    private void ClearSession()
    {
        _session?.Dispose();
        _session = null;
        context.SetSession(null);
    }

    public void Dispose()
    {
        ClearSession();
        GC.SuppressFinalize(this);
    }
}
