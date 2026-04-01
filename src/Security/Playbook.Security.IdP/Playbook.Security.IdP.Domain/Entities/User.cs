using System.Security.Cryptography;

using Playbook.Security.IdP.Domain.Entities.Base;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.Services;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

public sealed class User : AuditableEntity<UserId>
{
    private const int _maxFailedAttempts = 5;
    private const int _lockoutDurationMinutes = 15;

    // --- Core Identity (Now Value Objects) ---
    public Username Username { get; private set; }
    public Email Email { get; private set; }
    public bool IsEmailConfirmed { get; private set; }

    // --- Security Posture ---
    public PasswordHash PasswordHash { get; private set; }
    public string SecurityStamp { get; private set; }

    public bool IsTwoFactorEnabled { get; private set; }
    public int AccessFailedCount { get; private set; }
    public DateTime? LockoutEnd { get; private set; }

    // --- State & Lifecycle ---
    public UserStatus Status { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public byte[] RowVersion { get; private set; }

    public enum UserStatus { Pending, Active, Suspended, Archived }

    private User() { }

    private User(UserId id, Username username, Email email, PasswordHash passwordHash)
    {
        Id = id;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        SecurityStamp = NewSecurityStamp();
        Status = UserStatus.Active;
        IsActive = true;

        AddDomainEvent(new UserCreatedEvent(Id, Username.Value, Email.Value));
    }

    /// <summary>
    /// Gold Standard Factory: Orchestrates VOs and Services.
    /// </summary>
    public static User Create(
        string username,
        string email,
        string plainPassword,
        IPasswordHasher passwordHasher)
    {
        var userUsername = Username.Create(username);
        var userEmail = Email.Create(email);
        var hash = passwordHasher.HashPassword(plainPassword);

        return new User(UserId.New(), userUsername, userEmail, hash);
    }

    // --- Domain Behaviors ---

    public void RecordLoginSuccess()
    {
        EnsureCanLogin();

        LastLoginAt = DateTime.UtcNow;
        AccessFailedCount = 0;
        LockoutEnd = null;

        AddDomainEvent(new UserLoggedInEvent(Id, LastLoginAt.Value));
    }

    public void RecordLoginFailure()
    {
        if (IsLockedOut()) return;

        AccessFailedCount++;

        if (AccessFailedCount >= _maxFailedAttempts)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(_lockoutDurationMinutes);
            AddDomainEvent(new UserLockedOutEvent(Id, LockoutEnd.Value));
        }
    }

    public void UpdatePassword(string plainPassword, IPasswordHasher passwordHasher)
    {
        PasswordHash = passwordHasher.HashPassword(plainPassword);
        RefreshSecurityStamp();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPasswordChangedEvent(Id));
    }

    public void EnsureCanLogin()
    {
        if (Status != UserStatus.Active)
            throw new DomainException($"User is {Status}.", "USER_NOT_ACTIVE");

        if (IsLockedOut())
            throw new AccountLockedException(LockoutEnd!.Value);
    }

    public void EnableMfa()
    {
        if (IsTwoFactorEnabled) return;

        IsTwoFactorEnabled = true;
        RefreshSecurityStamp();

        AddDomainEvent(new UserMfaEnabledEvent(Id));
    }

    public void ConfirmEmail()
    {
        if (IsEmailConfirmed) return;
        IsEmailConfirmed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLockedOut() =>
        LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    private void RefreshSecurityStamp() => SecurityStamp = NewSecurityStamp();

    private static string NewSecurityStamp() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}
