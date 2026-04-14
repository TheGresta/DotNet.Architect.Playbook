using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Repositories;

public interface IUserClaimRepository : IRepository<UserClaim, UserClaimId>
{
}
