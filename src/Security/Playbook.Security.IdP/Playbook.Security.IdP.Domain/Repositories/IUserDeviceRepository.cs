using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Repositories;

public interface IUserDeviceRepository : IRepository<UserDevice, DeviceId>
{
    Task<IEnumerable<UserDevice>> GetByUserIdAsync(UserId userId, CancellationToken ct = default);
    Task<UserDevice?> GetByIdentityAsync(UserId userId, DeviceIdentity identity, CancellationToken ct = default);
}
