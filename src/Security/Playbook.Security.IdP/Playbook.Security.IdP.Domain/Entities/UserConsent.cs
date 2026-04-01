using Playbook.Security.IdP.Domain.Entities.Base;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

public sealed class UserConsent : AuditableEntity<UserConsentId>
{
    // --- Identity ---
    public UserId UserId { get; private set; }
    public ClientId ClientId { get; private set; } // Now a Value Object

    // --- State & Lifecycle ---
    private readonly List<ConsentScope> _scopes = new();
    public IReadOnlyCollection<ConsentScope> Scopes => _scopes.AsReadOnly();

    public ConsentStatus Status { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public enum ConsentStatus { Active, Revoked, Superseded }

    // Required for ORM
    private UserConsent() { }

    private UserConsent(
        UserConsentId id,
        UserId userId,
        ClientId clientId,
        IEnumerable<string> scopeNames,
        DateTime? expiresAt)
    {
        Id = id;
        UserId = userId;
        ClientId = clientId;
        ExpiresAt = expiresAt;
        Status = ConsentStatus.Active;
        IsActive = true;

        foreach (var name in scopeNames.Distinct())
        {
            _scopes.Add(new ConsentScope(name));
        }

        AddDomainEvent(new UserConsentGrantedEvent(UserId, ClientId.Value, scopeNames.ToList()));
    }

    /// <summary>
    /// Gold Standard Factory.
    /// </summary>
    public static UserConsent Create(
        UserId userId,
        string clientId,
        IEnumerable<string> scopes,
        TimeSpan? duration = null)
    {
        if (!scopes.Any())
            throw new DomainException("At least one scope must be granted.", "EMPTY_SCOPES");

        var client = ClientId.Create(clientId);
        var expiry = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : (DateTime?)null;

        return new UserConsent(UserConsentId.New(), userId, client, scopes, expiry);
    }

    // --- Domain Behaviors ---

    public void Revoke()
    {
        if (Status == ConsentStatus.Revoked) return;

        Status = ConsentStatus.Revoked;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserConsentRevokedEvent(UserId, ClientId.Value));
    }

    /// <summary>
    /// Updates the consent by adding new scopes. 
    /// Used when a user grants additional permissions to an existing app.
    /// </summary>
    public void AddScopes(IEnumerable<string> newScopeNames)
    {
        EnsureActive();

        foreach (var name in newScopeNames)
        {
            if (!_scopes.Any(s => s.Name == name.ToLowerInvariant().Trim()))
            {
                _scopes.Add(new ConsentScope(name));
            }
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifies if the consent is still valid and contains the required scope.
    /// </summary>
    public bool IsAuthorized(string scopeName)
    {
        if (Status != ConsentStatus.Active) return false;
        if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow) return false;

        return _scopes.Any(s => s.Name == scopeName.ToLowerInvariant().Trim());
    }

    private void EnsureActive()
    {
        if (Status != ConsentStatus.Active)
            throw new DomainException($"Consent is currently {Status}.", "CONSENT_NOT_ACTIVE");

        if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow)
            throw new DomainException("Consent has expired.", "CONSENT_EXPIRED");
    }
}
