using Playbook.Security.IdP.Domain.Entities.Base;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.ValueObjects.Ids;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Represents a high-security, stateful authentication attempt.
/// Governs the transition between credentials, MFA, and final token issuance.
/// </summary>
public sealed class AuthenticationSession : Entity<AuthenticationSessionId>
{
    // --- Identity Context ---
    public UserId UserId { get; private set; }
    public DeviceId DeviceId { get; private set; }
    public string CorrelationId { get; private set; } // Links to the OIDC Flow

    // --- State Machine ---
    public AuthStep Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // --- MFA Governance ---
    public string? MfaChallengeCode { get; private set; } // Hashed
    public int MfaAttemptCount { get; private set; }
    public DateTime? MfaLastAttemptAt { get; private set; }
    public bool IsMfaBypassedByTrust { get; private set; }

    // --- Metadata ---
    public string IpAddress { get; private set; }
    public AuthenticationRequirement Requirement { get; private set; }

    public enum AuthStep { Initialized, AwaitingMfa, Verified, Expired, Terminated }
    public enum AuthenticationRequirement { Standard, StepUp, PasswordReset }

    private AuthenticationSession() { }

    private AuthenticationSession(
        AuthenticationSessionId id,
        UserId userId,
        DeviceId deviceId,
        string correlationId,
        string ipAddress,
        AuthenticationRequirement requirement)
    {
        Id = id;
        UserId = userId;
        DeviceId = deviceId;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
        Requirement = requirement;
        Status = AuthStep.Initialized;
        ExpiresAt = DateTime.UtcNow.AddMinutes(5); // Short TTL for security

        AddDomainEvent(new AuthSessionStartedEvent(Id, UserId, CorrelationId));
    }

    /// <summary>
    /// Factory for starting a new authentication journey.
    /// </summary>
    public static AuthenticationSession Create(
        UserId userId,
        DeviceId deviceId,
        string correlationId,
        string ipAddress,
        bool isTrustedDevice)
    {
        var session = new AuthenticationSession(
            AuthenticationSessionId.New(),
            userId,
            deviceId,
            correlationId,
            ipAddress,
            AuthenticationRequirement.Standard);

        // If the device is already trusted, we might skip MFA depending on policy
        if (isTrustedDevice)
        {
            session.IsMfaBypassedByTrust = true;
            session.TransitionToVerified();
        }

        return session;
    }

    // --- Domain Behaviors ---

    /// <summary>
    /// Initiates an MFA challenge for this specific session.
    /// </summary>
    public void InitiateMfa(string hashedCode, TimeSpan ttl)
    {
        if (Status != AuthStep.Initialized)
            throw new DomainException("MFA can only be initiated from the Initialized state.");

        MfaChallengeCode = hashedCode;
        Status = AuthStep.AwaitingMfa;
        AddDomainEvent(new MfaChallengedEvent(Id, UserId));
    }

    /// <summary>
    /// Validates a provided MFA code with brute-force protection.
    /// </summary>
    public bool AttemptMfa(string providedCodeHash)
    {
        EnsureNotExpired();

        if (Status != AuthStep.AwaitingMfa)
            throw new DomainException("No active MFA challenge found.");

        if (MfaAttemptCount >= 3)
        {
            Status = AuthStep.Terminated;
            AddDomainEvent(new AuthSessionAbortedEvent(Id, UserId, "Too many MFA attempts."));
            return false;
        }

        MfaAttemptCount++;
        MfaLastAttemptAt = DateTime.UtcNow;

        if (MfaChallengeCode == providedCodeHash)
        {
            TransitionToVerified();
            return true;
        }

        return false;
    }

    private void TransitionToVerified()
    {
        Status = AuthStep.Verified;
        AddDomainEvent(new AuthSessionVerifiedEvent(Id, UserId, DeviceId));
    }

    private void EnsureNotExpired()
    {
        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = AuthStep.Expired;
            throw new DomainException("Authentication session has expired.");
        }
    }
}
