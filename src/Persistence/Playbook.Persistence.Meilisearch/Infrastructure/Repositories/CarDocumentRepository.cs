using Playbook.Persistence.Meilisearch.Core.Models;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

internal class CarDocumentRepository(MeiliContext context) : MeiliRepository<CarDocument>(context), ICarDocumentRepository
{
}
