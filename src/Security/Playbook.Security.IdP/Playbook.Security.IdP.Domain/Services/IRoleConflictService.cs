using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Services;

public interface IRoleConflictService
{
    Task AddConflictAsync(RoleId roleAId, RoleId roleBId, CancellationToken ct = default);
    Task RemoveConflictAsync(RoleId roleAId, RoleId roleBId, CancellationToken ct = default);
}
