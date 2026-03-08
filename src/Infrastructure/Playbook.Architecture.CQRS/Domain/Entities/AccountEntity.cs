using Playbook.Architecture.CQRS.Domain.Shared;

namespace Playbook.Architecture.CQRS.Domain.Entities;

public class AccountEntity
{
    public Guid Id { get; private set; }
    public decimal Balance { get; private set; }

    // Private constructor for EF Core or Factory methods
    private AccountEntity(Guid id, decimal initialBalance)
    {
        Id = id;
        Balance = initialBalance;
    }

    public static AccountEntity Create(decimal initialBalance) => new(Guid.NewGuid(), initialBalance);

    public Result Debit(decimal amount)
    {
        if (amount <= 0)
            return Result.Failure(Error.Validation("Account.InvalidAmount", "Amount must be positive."));

        if (Balance < amount)
            return Result.Failure(AccountErrors.InsufficientFunds);

        Balance -= amount;
        return Result.Success();
    }
}
