using Playbook.Security.IdP.Domain.Entities.Base;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.ValueObjects;
using Playbook.Security.IdP.Domain.ValueObjects.Ids;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Represents a user's explicit permission for a specific Client (Application) 
/// to access a defined set of Scopes.
/// </summary>
public sealed class UserConsent : AuditableEntity<UserConsentId>
{
    public UserId UserId { get; private set; }
    public string ClientId { get; private set; } // The OIDC Client ID

    // We store scopes as a Value Object to handle validation and normalization
    private readonly List<ConsentScope> _scopes = new();
    public IReadOnlyCollection<ConsentScope> Scopes => _scopes.AsReadOnly();

    public ConsentStatus Status { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public enum ConsentStatus { Active, Revoked, Superseded }

    private UserConsent() { }

    private UserConsent(
        UserConsentId id,
        UserId userId,
        string clientId,
        IEnumerable<string> scopeNames,
        DateTime? expiresAt)
    {
        Id = id;
        UserId = userId;
        ClientId = clientId;
        ExpiresAt = expiresAt;
        Status = ConsentStatus.Active;

        foreach (var name in scopeNames)
        {
            _scopes.Add(new ConsentScope(name));
        }

        AddDomainEvent(new UserConsentGrantedEvent(UserId, ClientId, scopeNames.ToList()));
    }

    /// <summary>
    /// Factory for creating or updating a grant.
    /// </summary>
    public static UserConsent Create(
        UserId userId,
        string clientId,
        IEnumerable<string> scopes,
        TimeSpan? duration = null)
    {
        var expiry = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : (DateTime?)null;
        return new UserConsent(UserConsentId.New(), userId, clientId, scopes, expiry);
    }

    // --- Domain Behaviors ---

    public void Revoke()
    {
        Status = ConsentStatus.Revoked;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UserConsentRevokedEvent(UserId, ClientId));
    }

    /// <summary>
    /// Checks if a specific scope is still authorized.
    /// </summary>
    public bool HasScope(string scopeName) =>
        Status == ConsentStatus.Active &&
        (ExpiresAt == null || ExpiresAt > DateTime.UtcNow) &&
        _scopes.Any(s => s.Name == scopeName);
}
