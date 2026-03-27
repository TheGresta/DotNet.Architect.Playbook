using Playbook.Persistence.Meilisearch.Core.Models;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

/// <summary>
/// Defines a specialized domain repository for <see cref="CarDocument"/> entities.
/// This interface extends the generic <see cref="IMeiliRepository{T}"/> to provide 
/// a strongly-typed abstraction specifically for automotive document operations.
/// </summary>
/// <remarks>
/// By defining a specific interface for the Car domain, this allows for 
/// easier dependency injection, mocking in unit tests, and the future 
/// addition of car-specific search methods (e.g., SearchByVinAsync) 
/// without polluting the generic base repository.
/// </remarks>
public interface ICarDocumentRepository : IMeiliRepository<CarDocument>
{
    // Inherits all CRUD and Search operations from IMeiliRepository<CarDocument>
}
