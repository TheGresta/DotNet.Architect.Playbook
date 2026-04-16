using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Repositories;

public interface IUserRepository : IRepository<User, UserId>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct = default);
}
