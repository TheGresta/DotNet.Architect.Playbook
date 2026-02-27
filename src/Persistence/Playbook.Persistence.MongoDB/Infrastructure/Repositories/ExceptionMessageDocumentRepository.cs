using Playbook.Persistence.MongoDB.Application.Repositories;
using Playbook.Persistence.MongoDB.Domain.Documents;
using Playbook.Persistence.MongoDB.Infrastructure.Contexts;

namespace Playbook.Persistence.MongoDB.Infrastructure.Repositories;

internal class ExceptionMessageDocumentRepository(MongoDbContext context) :
    BaseRepository<ExceptionMessageDocument>(context),
    IExceptionMessageDocumentRepository
{
}
