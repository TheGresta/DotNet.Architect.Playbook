namespace Playbook.Security.IdP.Domain.Exceptions;

public sealed class AccountLockedException : DomainException
{
    public DateTime LockedUntil { get; }

    public AccountLockedException(DateTime lockedUntil)
        : base($"Account is locked until {lockedUntil:yyyy-MM-dd HH:mm:ss} UTC.", "ACCOUNT_LOCKED")
    {
        LockedUntil = lockedUntil;
    }
}
