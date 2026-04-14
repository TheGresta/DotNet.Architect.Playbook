using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Tracks a single authentication journey from credential presentation to token issuance.
///
/// Design decisions:
/// - ClientId binding: OIDC requires the session to be bound to the relying party.
///   Issuing a token to a different client than the one that started the session
///   is a security violation.
/// - Nonce: Stored and bound to the issued ID Token to prevent replay attacks.
/// - AMR (Authentication Method References): Populated as each factor is satisfied.
///   The OIDC token issuer reads this to populate the "amr" claim per RFC 8176.
/// - PKCE: CodeChallenge and CodeChallengeMethod are stored here so the token
///   endpoint can validate the code_verifier without a DB round-trip to the
///   client registration.
/// - StepUp: RequiredAcr allows the session to demand a higher assurance level
///   mid-flow (e.g., step-up authentication for sensitive operations).
/// - MaxAttempts is a constant — never a magic number in logic.
/// </summary>
public sealed class AuthenticationSession : Entity<AuthenticationSessionId>
{
    private const int MaxMfaAttempts = 3;

    // ── Identity Context ──────────────────────────────────────────────────────
    public UserId UserId { get; private set; } = null!;
    public DeviceId DeviceId { get; private set; } = null!;
    public ClientId ClientId { get; private set; } = null!;  // OIDC relying party
    public CorrelationId CorrelationId { get; private set; } = null!;

    // ── OIDC / OAuth2 Binding ─────────────────────────────────────────────────
    /// <summary>Anti-replay nonce from the authorization request. Must be embedded in the ID Token.</summary>
    public string? Nonce { get; private set; }

    /// <summary>PKCE code challenge (S256 hash of the verifier).</summary>
    public string? CodeChallenge { get; private set; }
    public string? CodeChallengeMethod { get; private set; }  // "S256" | "plain"

    // ── State Machine ─────────────────────────────────────────────────────────
    public AuthStep Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // ── Authentication Methods ────────────────────────────────────────────────
    /// <summary>
    /// Tracks which factors have been satisfied. Maps to RFC 8176 "amr" claim values.
    /// e.g. ["pwd", "otp"], ["pwd", "hwk"], ["passkey"]
    /// </summary>
    private readonly List<string> _authMethodReferences = [];
    public IReadOnlyCollection<string> AuthMethodReferences => _authMethodReferences.AsReadOnly();

    /// <summary>
    /// The minimum ACR (Authentication Context Class Reference) required for this session.
    /// Null = default policy applies.
    /// </summary>
    public string? RequiredAcr { get; private set; }

    // ── MFA Governance ────────────────────────────────────────────────────────
    public MfaCodeHash? MfaChallengeCode { get; private set; }
    public int MfaAttemptCount { get; private set; }
    public DateTime? MfaLastAttemptAt { get; private set; }
    public bool IsMfaBypassedByTrust { get; private set; }
    public MfaMethod? MfaMethodUsed { get; private set; }

    // ── Risk & Network Context ────────────────────────────────────────────────
    public IpAddress IpAddress { get; private set; } = null!;
    public AuthenticationRequirement Requirement { get; private set; }

    /// <summary>One-time authorization code. Set on verification, consumed by token endpoint.</summary>
    public string? AuthorizationCode { get; private set; }
    public DateTime? AuthorizationCodeIssuedAt { get; private set; }

    // ── Enums ─────────────────────────────────────────────────────────────────
    public enum AuthStep
    {
        Initialized,
        AwaitingMfa,
        AwaitingPasskey,
        StepUpRequired,
        Verified,
        Expired,
        Terminated
    }

    public enum AuthenticationRequirement { Standard, StepUp, PasswordReset, Passkey }

    public enum MfaMethod { Totp, Sms, Email, BackupCode, Passkey }

    // ── ORM constructor ───────────────────────────────────────────────────────
    private AuthenticationSession() { }

    // ── Factory ───────────────────────────────────────────────────────────────
    private AuthenticationSession(
        AuthenticationSessionId id,
        UserId userId,
        DeviceId deviceId,
        ClientId clientId,
        CorrelationId correlationId,
        IpAddress ipAddress,
        string? nonce,
        string? codeChallenge,
        string? codeChallengeMethod,
        AuthenticationRequirement requirement)
    {
        Id = id;
        UserId = userId;
        DeviceId = deviceId;
        ClientId = clientId;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
        Nonce = nonce;
        CodeChallenge = codeChallenge;
        CodeChallengeMethod = codeChallengeMethod;
        Requirement = requirement;
        Status = AuthStep.Initialized;
        ExpiresAt = DateTime.UtcNow.AddMinutes(10);  // 10min window for full flow

        AddDomainEvent(new AuthSessionStartedEvent(Id, UserId, CorrelationId.Value));
    }

    /// <summary>
    /// Primary factory. All parameters are validated before the session is created.
    /// </summary>
    public static AuthenticationSession Create(
        UserId userId,
        DeviceId deviceId,
        string clientId,
        string correlationId,
        string ipAddress,
        bool isTrustedDevice,
        string? nonce = null,
        string? codeChallenge = null,
        string? codeChallengeMethod = null)
    {
        if (codeChallengeMethod is not null and not ("S256" or "plain"))
            throw new DomainException(
                "Only S256 and plain code challenge methods are supported.", "INVALID_PKCE_METHOD");

        var session = new AuthenticationSession(
            AuthenticationSessionId.New(),
            userId,
            deviceId,
            ClientId.Create(clientId),
            CorrelationId.Create(correlationId),
            IpAddress.Create(ipAddress),
            nonce,
            codeChallenge,
            codeChallengeMethod,
            AuthenticationRequirement.Standard);

        if (isTrustedDevice)
        {
            session.IsMfaBypassedByTrust = true;
            session._authMethodReferences.Add("hwk");  // Hardware key (trusted device)
            session.TransitionToVerified("pwd hwk");
        }

        return session;
    }

    // ── Domain Behaviours ─────────────────────────────────────────────────────

    public void RecordPasswordVerified()
    {
        EnsureNotExpiredOrTerminated();
        if (!_authMethodReferences.Contains("pwd"))
            _authMethodReferences.Add("pwd");
    }

    public void InitiateMfa(MfaCodeHash hashedCode, MfaMethod method)
    {
        EnsureNotExpiredOrTerminated();

        if (Status != AuthStep.Initialized)
            throw new DomainException("MFA can only be initiated once per session.", "MFA_ALREADY_INITIATED");

        MfaChallengeCode = hashedCode;
        MfaMethodUsed = method;
        Status = AuthStep.AwaitingMfa;

        AddDomainEvent(new MfaChallengedEvent(Id, UserId));
    }

    public bool AttemptMfa(MfaCodeHash providedCodeHash)
    {
        EnsureNotExpiredOrTerminated();

        if (Status != AuthStep.AwaitingMfa)
            throw new DomainException("No active MFA challenge for this session.", "MFA_NOT_ACTIVE");

        if (MfaAttemptCount >= MaxMfaAttempts)
        {
            Terminate("Exceeded maximum MFA attempts.");
            return false;
        }

        MfaAttemptCount++;
        MfaLastAttemptAt = DateTime.UtcNow;

        if (MfaChallengeCode != providedCodeHash)
            return false;

        var amrValue = MfaMethodUsed switch
        {
            MfaMethod.Totp => "otp",
            MfaMethod.Sms => "sms",
            MfaMethod.Email => "email",
            MfaMethod.BackupCode => "otp",  // Treated as OTP per RFC 8176
            MfaMethod.Passkey => "hwk",
            _ => "otp"
        };

        if (!_authMethodReferences.Contains(amrValue))
            _authMethodReferences.Add(amrValue);

        TransitionToVerified(string.Join(" ", _authMethodReferences));
        return true;
    }

    public void InitiatePasskeyAssertion()
    {
        EnsureNotExpiredOrTerminated();

        if (Status != AuthStep.Initialized)
            throw new DomainException("Passkey assertion can only be initiated once.", "PASSKEY_ALREADY_INITIATED");

        Status = AuthStep.AwaitingPasskey;
    }

    public void RecordPasskeyAssertion()
    {
        EnsureNotExpiredOrTerminated();

        if (Status != AuthStep.AwaitingPasskey)
            throw new DomainException("No active passkey assertion for this session.", "PASSKEY_NOT_ACTIVE");

        if (!_authMethodReferences.Contains("hwk"))
            _authMethodReferences.Add("hwk");

        TransitionToVerified("hwk");
    }

    /// <summary>
    /// Generates and seals a one-time authorization code for the token endpoint.
    /// Can only be called once — codes are not re-issuable (prevents code reuse).
    /// </summary>
    public string IssueAuthorizationCode()
    {
        if (Status != AuthStep.Verified)
            throw new DomainException("Session must be verified before issuing a code.", "SESSION_NOT_VERIFIED");

        if (AuthorizationCode is not null)
            throw new DomainException("Authorization code has already been issued.", "CODE_ALREADY_ISSUED");

        // High-entropy single-use code (32 bytes = 256 bits)
        var code = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        AuthorizationCode = code;
        AuthorizationCodeIssuedAt = DateTime.UtcNow;

        return code;
    }

    public void Terminate(string reason)
    {
        Status = AuthStep.Terminated;
        AddDomainEvent(new AuthSessionAbortedEvent(Id, UserId, reason));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void TransitionToVerified(string amrSummary)
    {
        Status = AuthStep.Verified;
        AddDomainEvent(new AuthSessionVerifiedEvent(Id, UserId, DeviceId));
    }

    private void EnsureNotExpiredOrTerminated()
    {
        if (Status == AuthStep.Terminated)
            throw new DomainException("Session has been terminated.", "SESSION_TERMINATED");

        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = AuthStep.Expired;
            throw new DomainException(
                "Authentication session has expired. Please restart the login flow.",
                "SESSION_EXPIRED");
        }
    }
}
