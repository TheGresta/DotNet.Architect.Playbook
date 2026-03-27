using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

/// <summary>
/// Provides a concrete, domain-specific implementation of the <see cref="ICarDocumentRepository"/>.
/// This class inherits the high-performance generic logic from <see cref="MeiliRepository{CarDocument}"/> 
/// and maps it to the <see cref="CarDocument"/> index defined in the <see cref="MeiliContext"/>.
/// </summary>
/// <remarks>
/// The repository pattern here decouples the automotive domain logic from the underlying 
/// Meilisearch SDK, allowing for a clean, testable architecture. 
/// Any car-specific search optimizations or custom scoring logic should be implemented here.
/// </remarks>
internal class CarDocumentRepository(MeiliContext context)
    : MeiliRepository<CarDocument>(context), ICarDocumentRepository
{
    // The constructor uses C# 12 primary constructor syntax to pass the 
    // MeiliContext dependency directly to the base MeiliRepository.
}
