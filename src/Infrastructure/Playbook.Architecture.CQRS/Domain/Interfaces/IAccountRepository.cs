using Playbook.Architecture.CQRS.Domain.Entities;

namespace Playbook.Architecture.CQRS.Domain.Interfaces;

public interface IAccountRepository
{
    Task<AccountEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<AccountEntity>> GetAccountsAsync(CancellationToken ct = default);
    void Add(AccountEntity account);
}
