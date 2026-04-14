using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

public sealed class AuthenticationSession : Entity<AuthenticationSessionId>
{
    // --- Identity Context ---
    public UserId UserId { get; private set; }
    public DeviceId DeviceId { get; private set; }
    public CorrelationId CorrelationId { get; private set; }

    // --- State Machine ---
    public AuthStep Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // --- MFA Governance ---
    public MfaCodeHash? MfaChallengeCode { get; private set; }
    public int MfaAttemptCount { get; private set; }
    public DateTime? MfaLastAttemptAt { get; private set; }
    public bool IsMfaBypassedByTrust { get; private set; }

    // --- Metadata ---
    public IpAddress IpAddress { get; private set; }
    public AuthenticationRequirement Requirement { get; private set; }

    public enum AuthStep { Initialized, AwaitingMfa, Verified, Expired, Terminated }
    public enum AuthenticationRequirement { Standard, StepUp, PasswordReset }

    private AuthenticationSession() { }

    private AuthenticationSession(
        AuthenticationSessionId id,
        UserId userId,
        DeviceId deviceId,
        CorrelationId correlationId,
        IpAddress ipAddress,
        AuthenticationRequirement requirement)
    {
        Id = id;
        UserId = userId;
        DeviceId = deviceId;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
        Requirement = requirement;
        Status = AuthStep.Initialized;
        ExpiresAt = DateTime.UtcNow.AddMinutes(5);

        AddDomainEvent(new AuthSessionStartedEvent(Id, UserId, CorrelationId.Value));
    }

    /// <summary>
    /// Gold Standard Factory for starting an auth journey.
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
            CorrelationId.Create(correlationId),
            IpAddress.Create(ipAddress),
            AuthenticationRequirement.Standard);

        if (isTrustedDevice)
        {
            session.IsMfaBypassedByTrust = true;
            session.TransitionToVerified();
        }

        return session;
    }

    // --- Domain Behaviors ---

    public void InitiateMfa(MfaCodeHash hashedCode)
    {
        EnsureNotExpired();

        if (Status != AuthStep.Initialized)
            throw new DomainException("MFA can only be initiated once per session.", "MFA_ALREADY_INITIATED");

        MfaChallengeCode = hashedCode;
        Status = AuthStep.AwaitingMfa;

        AddDomainEvent(new MfaChallengedEvent(Id, UserId));
    }

    public bool AttemptMfa(MfaCodeHash providedCodeHash)
    {
        EnsureNotExpired();

        if (Status != AuthStep.AwaitingMfa)
            throw new DomainException("No active MFA challenge found for this session.", "MFA_NOT_ACTIVE");

        if (MfaAttemptCount >= 3)
        {
            Terminate("Exceeded maximum MFA attempts.");
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

    public void Terminate(string reason)
    {
        Status = AuthStep.Terminated;
        AddDomainEvent(new AuthSessionAbortedEvent(Id, UserId, reason));
    }

    private void TransitionToVerified()
    {
        Status = AuthStep.Verified;
        AddDomainEvent(new AuthSessionVerifiedEvent(Id, UserId, DeviceId));
    }

    private void EnsureNotExpired()
    {
        if (Status == AuthStep.Terminated)
            throw new DomainException("Session has been terminated.", "SESSION_TERMINATED");

        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = AuthStep.Expired;
            throw new DomainException("Authentication session has expired. Please restart login.", "SESSION_EXPIRED");
        }
    }
}
