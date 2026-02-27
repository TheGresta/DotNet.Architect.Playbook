using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Playbook.Persistence.MongoDB.Application;
using Playbook.Persistence.MongoDB.Domain;
using Playbook.Persistence.MongoDB.Infrastructure.Contexts;
using Playbook.Persistence.MongoDB.Infrastructure.Extensions;

namespace Playbook.Persistence.MongoDB.Infrastructure;

/// <summary>
/// Generic repository providing a standard interface for CRUD operations.
/// Handles session management for transactions and optimistic concurrency control.
/// </summary>
internal class BaseRepository<TDocument>(MongoDbContext context) : IBaseRepository<TDocument>
    where TDocument : BaseDocument
{
    // Summary: The specific MongoDB collection mapped to the document type name.
    protected readonly IMongoCollection<TDocument> Collection =
        context.Database.GetCollection<TDocument>(typeof(TDocument).Name);

    #region Queries

    public async Task<TDocument?> FindOneAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken ct)
        => await Collection.Find(predicate).FirstOrDefaultAsync(ct);

    /// <summary>
    /// Fetches a paged result set based on a filter and sort order.
    /// </summary>
    public async Task<Paginate<TDocument>> FindAllByPaginateAsync(
        Expression<Func<TDocument, bool>>? predicate,
        Func<IQueryable<TDocument>, IOrderedQueryable<TDocument>>? orderBy,
        int index, int size,
        CancellationToken ct)
    {
        IQueryable<TDocument> query = ApplyQueryFeatures(predicate, orderBy);
        return await query.ToPaginateAsync(index, size, ct);
    }

    public async Task<List<TDocument>> FindAllAsync(
        Expression<Func<TDocument, bool>>? predicate,
        Func<IQueryable<TDocument>, IOrderedQueryable<TDocument>>? orderBy,
        int? takeTop,
        CancellationToken ct)
    {
        IQueryable<TDocument> query = ApplyQueryFeatures(predicate, orderBy);

        if (takeTop.HasValue)
        {
            query = query.Take(takeTop.Value);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<bool> AnyAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken ct)
        => await Collection.Find(predicate).AnyAsync(ct);

    public async Task<long> CountAsync(Expression<Func<TDocument, bool>>? predicate, CancellationToken ct)
        => await Collection.CountDocumentsAsync(predicate ?? (_ => true), cancellationToken: ct);

    #endregion

    #region Commands

    /// <summary>
    /// Inserts a single document. Automatically sets CreatedAt if not provided.
    /// Uses the active transaction session if one is available.
    /// </summary>
    public async Task AddAsync(TDocument document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.CreatedAt == default)
        {
            document.CreatedAt = DateTime.UtcNow;
        }

        if (context.Session != null)
        {
            await Collection.InsertOneAsync(context.Session, document, cancellationToken: ct);
        }
        else
        {
            await Collection.InsertOneAsync(document, cancellationToken: ct);
        }
    }

    public async Task AddRangeAsync(IEnumerable<TDocument> documents, CancellationToken ct)
    {
        var docs = documents.ToList();
        if (docs.Count == 0)
        {
            return;
        }

        if (context.Session != null)
        {
            await Collection.InsertManyAsync(context.Session, docs, cancellationToken: ct);
        }
        else
        {
            await Collection.InsertManyAsync(docs, cancellationToken: ct);
        }
    }

    /// <summary>
    /// Replaces an existing document. 
    /// Includes a version check to prevent "Lost Updates" (Optimistic Concurrency).
    /// </summary>
    public async Task UpdateAsync(TDocument document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);

        var currentVersion = document.Version;
        document.Version++;

        // Summary: Matches both Id AND the version we fetched to ensure no one else modified it in between.
        Expression<Func<TDocument, bool>> filter = x => x.Id == document.Id && x.Version == currentVersion;

        ReplaceOneResult result;
        if (context.Session != null)
        {
            result = await Collection.ReplaceOneAsync(context.Session, filter, document, new ReplaceOptions { IsUpsert = false }, ct);
        }
        else
        {
            result = await Collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = false }, ct);
        }

        if (result.ModifiedCount == 0)
        {
            throw new InvalidOperationException($"Concurrency conflict: Document {document.Id} was modified or does not exist.");
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        if (context.Session != null)
        {
            await Collection.DeleteOneAsync(context.Session, x => x.Id == id, cancellationToken: ct);
        }
        else
        {
            await Collection.DeleteOneAsync(x => x.Id == id, cancellationToken: ct);
        }
    }

    public async Task DeleteAsync(TDocument document, CancellationToken ct)
        => await DeleteAsync(document.Id, ct);

    #endregion

    /// <summary>
    /// Build an IQueryable with filters and sorting before execution.
    /// This allows the LINQ provider to optimize the final BSON query.
    /// </summary>
    private IQueryable<TDocument> ApplyQueryFeatures(
        Expression<Func<TDocument, bool>>? predicate,
        Func<IQueryable<TDocument>, IOrderedQueryable<TDocument>>? orderBy)
    {
        IQueryable<TDocument> query = Collection.AsQueryable();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return query;
    }
}
