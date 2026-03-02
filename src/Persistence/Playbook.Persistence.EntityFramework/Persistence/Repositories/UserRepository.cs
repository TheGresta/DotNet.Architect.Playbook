using Playbook.Persistence.EntityFramework.Application.Repositories;
using Playbook.Persistence.EntityFramework.Domain;
using Playbook.Persistence.EntityFramework.Persistence.Context;

namespace Playbook.Persistence.EntityFramework.Persistence.Repositories;

internal class UserRepository(ApplicationDbContext context) : BaseRepository<UserEntity>(context), IUserRepository
{
}
