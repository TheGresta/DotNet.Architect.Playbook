using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Records a user's grant of consent for a specific OAuth2 client to access
/// a set of resource scopes on their behalf.
///
/// Design decisions:
/// - RedirectUri binding: RFC 6749 §10.6 — the consent is bound to the exact
///   redirect URI used in the authorization request. Consent for a different URI
///   must be explicitly re-granted.
/// - PKCE: The consent records whether PKCE was used so the token endpoint can
///   enforce it on subsequent token refreshes.
/// - Superseded state: When a user expands a previous consent, the old record
///   is marked Superseded and a new one is created, providing a full audit trail
///   of consent changes.
/// - ExpiresAt guard: Validated at creation so no expired consent can be persisted.
/// </summary>
public sealed class UserConsent : AuditableEntity<UserConsentId>
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public UserId UserId { get; private set; } = null!;
    public ClientId ClientId { get; private set; } = null!;

    // ── OAuth2 Binding ────────────────────────────────────────────────────────
    /// <summary>
    /// The exact redirect URI used in the authorization request.
    /// Stored to prevent consent for one URI being used for another.
    /// </summary>
    public string RedirectUri { get; private set; } = string.Empty;

    /// <summary>Whether PKCE was used in the authorization request.</summary>
    public bool WasPkceUsed { get; private set; }

    // ── State & Lifecycle ─────────────────────────────────────────────────────
    private readonly List<ConsentScope> _scopes = [];
    public IReadOnlyCollection<ConsentScope> Scopes => _scopes.AsReadOnly();

    public ConsentStatus Status { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    // ── Audit ─────────────────────────────────────────────────────────────────
    /// <summary>IP address at the time of consent grant (for audit / anomaly detection).</summary>
    public string? GrantedFromIp { get; private set; }

    /// <summary>Points to the consent record that this one superseded.</summary>
    public UserConsentId? SupersededConsentId { get; private set; }

    public enum ConsentStatus { Active, Revoked, Superseded, Expired }

    // ── ORM constructor ───────────────────────────────────────────────────────
    private UserConsent() { }

    // ── Private constructor ───────────────────────────────────────────────────
    private UserConsent(
        UserConsentId id,
        UserId userId,
        ClientId clientId,
        IEnumerable<string> scopeNames,
        string redirectUri,
        bool wasPkceUsed,
        string? grantedFromIp,
        DateTime? expiresAt)
    {
        Id = id;
        UserId = userId;
        ClientId = clientId;
        RedirectUri = redirectUri;
        WasPkceUsed = wasPkceUsed;
        GrantedFromIp = grantedFromIp;
        ExpiresAt = expiresAt;
        Status = ConsentStatus.Active;
        IsActive = true;

        foreach (var name in scopeNames.Distinct())
            _scopes.Add(new ConsentScope(name));

        AddDomainEvent(new UserConsentGrantedEvent(UserId, ClientId.Value, _scopes.Select(s => s.Name).ToList()));
    }

    /// <summary>
    /// Primary factory. All OAuth2 authorization request parameters are captured
    /// so the consent is fully bound to the original request context.
    /// </summary>
    public static UserConsent Create(
        UserId userId,
        string clientId,
        IEnumerable<string> scopes,
        string redirectUri,
        bool wasPkceUsed = false,
        string? grantedFromIp = null,
        TimeSpan? duration = null)
    {
        var scopeList = scopes.ToList();

        if (!scopeList.Any())
            throw new DomainException("At least one scope must be granted.", "EMPTY_SCOPES");

        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new DomainException("Redirect URI is required for consent binding.", "MISSING_REDIRECT_URI");

        if (duration.HasValue && duration.Value <= TimeSpan.Zero)
            throw new DomainException("Consent duration must be a positive value.", "INVALID_DURATION");

        var client = ClientId.Create(clientId);
        var expiry = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : (DateTime?)null;

        return new UserConsent(
            UserConsentId.New(), userId, client, scopeList,
            redirectUri.Trim(), wasPkceUsed, grantedFromIp, expiry);
    }

    // ── Domain Behaviours ─────────────────────────────────────────────────────

    public void Revoke()
    {
        if (Status == ConsentStatus.Revoked) return;

        Status = ConsentStatus.Revoked;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserConsentRevokedEvent(UserId, ClientId.Value));
    }

    public void MarkSuperseded(UserConsentId newConsentId)
    {
        Status = ConsentStatus.Superseded;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserConsentSupersededEvent(UserId, ClientId.Value, newConsentId));
    }

    /// <summary>
    /// Adds new scopes to the consent. Used when the user grants additional
    /// permissions to an already-authorized client.
    /// </summary>
    public void AddScopes(IEnumerable<string> newScopeNames)
    {
        EnsureActive();

        foreach (var name in newScopeNames)
        {
            var normalized = name.ToLowerInvariant().Trim();
            if (!_scopes.Any(s => s.Name == normalized))
                _scopes.Add(new ConsentScope(name));
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks whether this consent is valid, not expired, and covers the requested scope.
    /// The redirect URI must also match — a scope check against the wrong URI fails.
    /// </summary>
    public bool IsAuthorized(string scopeName, string redirectUri)
    {
        if (Status != ConsentStatus.Active) return false;
        if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow) return false;
        if (!string.Equals(RedirectUri, redirectUri.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        return _scopes.Any(s => s.Name == scopeName.ToLowerInvariant().Trim());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnsureActive()
    {
        if (Status != ConsentStatus.Active)
            throw new DomainException($"Consent is currently {Status}.", "CONSENT_NOT_ACTIVE");

        if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow)
        {
            Status = ConsentStatus.Expired;
            throw new DomainException("Consent has expired.", "CONSENT_EXPIRED");
        }
    }
}
