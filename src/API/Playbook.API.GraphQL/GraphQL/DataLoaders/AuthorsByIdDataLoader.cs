namespace Playbook.API.GraphQL.GraphQL.DataLoaders;

// Solves the N+1 problem: when fetching N books with their authors within a
// single request, all AuthorId lookups are batched into one GetByIdsAsync call.
public sealed class AuthorsByIdDataLoader(
    IAuthorRepository repository,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, Author>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<Guid, Author>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        var authors = await repository.GetByIdsAsync(keys, cancellationToken);
        return authors.ToDictionary(a => a.Id);
    }
}
