using System.Security.Cryptography;

using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.Services;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// The User Aggregate Root. Owns all identity, credential, and security posture state.
///
/// Design decisions:
/// - All credential mutations are in-aggregate (no anemic setters).
/// - SecurityStamp is refreshed on every credential or trust-boundary change, invalidating
///   all existing tokens that embed it.
/// - RowVersion is managed by the persistence layer via EF Core concurrency tokens —
///   the domain does not set it, but declares it for conflict detection.
/// - MFA is modelled as a pluggable collection so TOTP, SMS, and FIDO2 co-exist.
/// </summary>
public sealed class User : AuditableEntity<UserId>
{
    // ── Constants ───────────────────────────────────────────────────────────
    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationMinutes = 15;
    private const int RecoveryCodeCount = 10;

    // ── Core Identity ────────────────────────────────────────────────────────
    public Username Username { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public bool IsEmailConfirmed { get; private set; }

    /// <summary>Phone number stored normalized (E.164 format).</summary>
    public PhoneNumber? PhoneNumber { get; private set; }
    public bool IsPhoneConfirmed { get; private set; }

    // ── Credentials ──────────────────────────────────────────────────────────
    public PasswordHash PasswordHash { get; private set; } = null!;

    /// <summary>
    /// Changes whenever credentials or trust boundaries change.
    /// JWT/cookie validators compare this stamp to detect token invalidation.
    /// </summary>
    public string SecurityStamp { get; private set; } = null!;

    // ── MFA State ────────────────────────────────────────────────────────────
    public bool IsTwoFactorEnabled { get; private set; }

    /// <summary>AES-256 encrypted TOTP seed. Null if authenticator app not configured.</summary>
    public string? TotpSecretEncrypted { get; private set; }

    /// <summary>Hashed backup recovery codes. Consumed on use.</summary>
    private readonly List<RecoveryCode> _recoveryCodes = [];
    public IReadOnlyCollection<RecoveryCode> RecoveryCodes => _recoveryCodes.AsReadOnly();

    // ── Passkey / FIDO2 ──────────────────────────────────────────────────────
    private readonly List<UserPasskey> _passkeys = [];
    public IReadOnlyCollection<UserPasskey> Passkeys => _passkeys.AsReadOnly();

    // ── External Identity Providers ──────────────────────────────────────────
    private readonly List<ExternalLogin> _externalLogins = [];
    public IReadOnlyCollection<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

    // ── Security Posture ─────────────────────────────────────────────────────
    public int AccessFailedCount { get; private set; }
    public DateTime? LockoutEnd { get; private set; }

    // ── State & Lifecycle ────────────────────────────────────────────────────
    public UserStatus Status { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// EF Core optimistic-concurrency token. Populated by the DB on every write.
    /// Declared here so the domain can be hydrated by the persistence layer.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    // ── RBAC ─────────────────────────────────────────────────────────────────
    private readonly List<UserRole> _roles = [];
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private readonly List<UserClaim> _claims = [];
    public IReadOnlyCollection<UserClaim> Claims => _claims.AsReadOnly();

    public enum UserStatus { Pending, Active, Suspended, Archived }

    // ── ORM constructor ───────────────────────────────────────────────────────
    private User() { }

    // ── Factory ───────────────────────────────────────────────────────────────
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
    /// Primary factory. Validates value objects and hashes the password before
    /// assembling the aggregate — no raw strings escape to the caller.
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

    // ── Domain Behaviours: Authentication ─────────────────────────────────────

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
        // Do not increment if already locked — prevents counter drift.
        if (IsLockedOut()) return;

        if (LockoutEnd.HasValue && LockoutEnd.Value <= DateTime.UtcNow)
        {
            LockoutEnd = null;
            AccessFailedCount = 0;
        }

        AccessFailedCount++;

        if (AccessFailedCount >= MaxFailedAttempts)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
            AddDomainEvent(new UserLockedOutEvent(Id, LockoutEnd.Value));
        }
    }

    /// <summary>
    /// Guard called by login flow and token issuance. Throws typed exceptions
    /// so the Application layer can map them to specific HTTP responses.
    /// </summary>
    public void EnsureCanLogin()
    {
        if (Status == UserStatus.Archived)
            throw new DomainException("This account has been archived.", "USER_ARCHIVED");

        if (Status == UserStatus.Suspended)
            throw new DomainException("This account is suspended. Contact support.", "USER_SUSPENDED");

        if (Status != UserStatus.Active)
            throw new DomainException($"Account is not active (status: {Status}).", "USER_NOT_ACTIVE");

        if (IsLockedOut())
            throw new AccountLockedException(LockoutEnd!.Value);
    }

    // ── Domain Behaviours: Credentials ────────────────────────────────────────

    public void UpdatePassword(string plainPassword, IPasswordHasher passwordHasher)
    {
        PasswordHash = passwordHasher.HashPassword(plainPassword);
        RefreshSecurityStamp();   // Invalidate all existing sessions/tokens.
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPasswordChangedEvent(Id));
    }

    // ── Domain Behaviours: Email ──────────────────────────────────────────────

    public void ConfirmEmail()
    {
        if (IsEmailConfirmed) return;
        IsEmailConfirmed = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserEmailConfirmedEvent(Id, Email.Value));
    }

    public void ChangeEmail(string newEmail)
    {
        var updated = Email.Create(newEmail);

        if (Email == updated) return;

        Email = updated;
        IsEmailConfirmed = false;      // Requires re-confirmation.
        RefreshSecurityStamp();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserEmailChangedEvent(Id, Email.Value));
    }

    // ── Domain Behaviours: Phone ──────────────────────────────────────────────

    public void SetPhone(string e164Number)
    {
        PhoneNumber = PhoneNumber.Create(e164Number);
        IsPhoneConfirmed = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmPhone()
    {
        if (PhoneNumber is null)
            throw new DomainException("Cannot confirm a phone number before one is set.", "PHONE_NOT_SET");

        if (IsPhoneConfirmed) return;
        IsPhoneConfirmed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Domain Behaviours: MFA ────────────────────────────────────────────────

    public void EnableMfa()
    {
        var hasFactor =
            !string.IsNullOrWhiteSpace(TotpSecretEncrypted) ||
            (PhoneNumber is not null && IsPhoneConfirmed) ||
            _passkeys.Count > 0;

        if (!hasFactor)
            throw new DomainException("Configure a factor before enabling MFA.", "MFA_FACTOR_REQUIRED");

        if (IsTwoFactorEnabled) return;

        IsTwoFactorEnabled = true;
        RefreshSecurityStamp();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserMfaEnabledEvent(Id));
    }

    public void DisableMfa()
    {
        if (!IsTwoFactorEnabled) return;

        IsTwoFactorEnabled = false;
        TotpSecretEncrypted = null;
        _recoveryCodes.Clear();
        RefreshSecurityStamp();

        AddDomainEvent(new UserMfaDisabledEvent(Id));
    }

    /// <param name="encryptedSecret">AES-256 encrypted TOTP seed — never plaintext.</param>
    public void SetTotpSecret(string encryptedSecret)
    {
        if (string.IsNullOrWhiteSpace(encryptedSecret))
            throw new DomainException("TOTP secret cannot be empty.", "INVALID_TOTP_SECRET");

        TotpSecretEncrypted = encryptedSecret;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generates and stores hashed recovery codes. Returns the plaintext codes
    /// exactly once — the caller must display them immediately.
    /// </summary>
    public IReadOnlyList<string> GenerateRecoveryCodes(IPasswordHasher hasher)
    {
        _recoveryCodes.Clear();

        var plaintextCodes = Enumerable.Range(0, RecoveryCodeCount)
            .Select(_ => GenerateRecoveryCodePlaintext())
            .ToList();

        foreach (var code in plaintextCodes)
        {
            var hashed = hasher.HashPassword(code);
            _recoveryCodes.Add(new RecoveryCode(Id, PasswordHash.FromString(hashed.Value)));
        }

        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserRecoveryCodesGeneratedEvent(Id));

        return plaintextCodes;
    }

    /// <summary>
    /// Attempts to consume a recovery code. Each code is single-use.
    /// Returns false if the code is invalid or already consumed.
    /// </summary>
    public bool TryConsumeRecoveryCode(string plaintextCode, IPasswordHasher hasher)
    {
        var match = _recoveryCodes
            .Where(rc => !rc.IsConsumed)
            .FirstOrDefault(rc => hasher.VerifyPassword(plaintextCode, rc.CodeHash));

        if (match is null) return false;

        match.Consume();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserRecoveryCodeConsumedEvent(Id));

        return true;
    }

    // ── Domain Behaviours: Passkeys ───────────────────────────────────────────

    public void RegisterPasskey(
        string credentialId,
        string publicKeyJwk,
        string aaguid,
        string deviceName)
    {
        if (_passkeys.Any(p => p.CredentialId == credentialId))
            throw new DomainException("Passkey credential is already registered.", "PASSKEY_DUPLICATE");

        var passkey = new UserPasskey(Id, credentialId, publicKeyJwk, aaguid, deviceName);
        _passkeys.Add(passkey);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPasskeyRegisteredEvent(Id, credentialId, deviceName));
    }

    public void RemovePasskey(string credentialId)
    {
        var passkey = _passkeys.FirstOrDefault(p => p.CredentialId == credentialId)
            ?? throw new DomainException("Passkey not found.", "PASSKEY_NOT_FOUND");

        _passkeys.Remove(passkey);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPasskeyRemovedEvent(Id, credentialId));
    }

    // ── Domain Behaviours: External Logins ───────────────────────────────────

    public void LinkExternalLogin(string provider, string providerSubjectId, string? providerEmail)
    {
        if (_externalLogins.Any(e => e.Provider == provider && e.ProviderSubjectId == providerSubjectId))
            throw new DomainException("This external account is already linked.", "EXTERNAL_LOGIN_DUPLICATE");

        _externalLogins.Add(new ExternalLogin(Id, provider, providerSubjectId, providerEmail));
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserExternalLoginLinkedEvent(Id, provider, providerSubjectId));
    }

    public void UnlinkExternalLogin(string provider, string providerSubjectId)
    {
        var login = _externalLogins.FirstOrDefault(
            e => e.Provider == provider && e.ProviderSubjectId == providerSubjectId)
            ?? throw new DomainException("External login not found.", "EXTERNAL_LOGIN_NOT_FOUND");

        // Guard: must not remove last login method if no password is set.
        if (!_externalLogins.Any(e => e != login) && string.IsNullOrEmpty(PasswordHash.Value))
            throw new DomainException(
                "Cannot remove the last login method. Please set a password first.",
                "LAST_LOGIN_METHOD");

        _externalLogins.Remove(login);
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Domain Behaviours: RBAC ───────────────────────────────────────────────

    public void AssignRole(RoleId roleId, UserId grantedBy, DateTime? expiresAt = null)
    {
        if (_roles.Any(r => r.RoleId == roleId && !r.IsExpired()))
            return; // Idempotent — do not add duplicate active assignment.

        _roles.Add(new UserRole(Id, roleId, grantedBy, expiresAt));
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserRoleAssignedEvent(Id, roleId, grantedBy));
    }

    public void RevokeRole(RoleId roleId)
    {
        var role = _roles.FirstOrDefault(r => r.RoleId == roleId && !r.IsExpired())
            ?? throw new DomainException("Role assignment not found.", "ROLE_NOT_ASSIGNED");

        _roles.Remove(role);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserRoleRevokedEvent(Id, roleId));
    }

    public void AddClaim(string type, string value, UserClaim.ValueTypes valueType = UserClaim.ValueTypes.String)
    {
        _claims.Add(new UserClaim(Id, type, value, valueType));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveClaims(string type)
    {
        _claims.RemoveAll(c => c.Type == type);
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Domain Behaviours: Admin ──────────────────────────────────────────────

    public void Suspend(string reason)
    {
        if (Status == UserStatus.Suspended) return;

        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
        RefreshSecurityStamp();

        AddDomainEvent(new UserSuspendedEvent(Id, reason));
    }

    public void Reinstate()
    {
        if (Status != UserStatus.Suspended)
            throw new DomainException("User is not suspended.", "USER_NOT_SUSPENDED");

        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserReinstatedEvent(Id));
    }

    public void UnlockAccount()
    {
        if (!IsLockedOut()) return;

        LockoutEnd = null;
        AccessFailedCount = 0;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserUnlockedEvent(Id));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public bool IsLockedOut() =>
        LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    private void RefreshSecurityStamp() =>
        SecurityStamp = NewSecurityStamp();

    private static string NewSecurityStamp() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    private static string GenerateRecoveryCodePlaintext()
    {
        // Format: XXXXX-XXXXX (alphanumeric, case-insensitive)
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(10);
        var part1 = new string(bytes.Take(5).Select(b => chars[b % chars.Length]).ToArray());
        var part2 = new string(bytes.Skip(5).Select(b => chars[b % chars.Length]).ToArray());
        return $"{part1}-{part2}";
    }
}
