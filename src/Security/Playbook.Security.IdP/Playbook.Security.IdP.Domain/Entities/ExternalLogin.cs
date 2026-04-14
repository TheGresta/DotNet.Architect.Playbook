using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Tracks an OAuth2/OIDC external identity provider link for a user account.
/// Supports federation with Google, GitHub, Azure AD, etc.
/// </summary>
public sealed class ExternalLogin : Entity<ExternalLoginId>
{
    public UserId UserId { get; private set; } = null!;

    /// <summary>e.g. "google", "github", "azure-ad"</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>The "sub" claim value from the external provider's token.</summary>
    public string ProviderSubjectId { get; private set; } = string.Empty;

    /// <summary>Email as reported by the external provider at link time (informational).</summary>
    public string? ProviderEmail { get; private set; }

    public DateTime LinkedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    // ORM constructor
    private ExternalLogin() { }

    internal ExternalLogin(UserId userId, string provider, string providerSubjectId, string? providerEmail)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new DomainException("External login provider cannot be empty.", "INVALID_PROVIDER");

        if (string.IsNullOrWhiteSpace(providerSubjectId))
            throw new DomainException("Provider subject ID cannot be empty.", "INVALID_SUBJECT_ID");

        Id = ExternalLoginId.New();
        UserId = userId;
        Provider = provider.ToLowerInvariant().Trim();
        ProviderSubjectId = providerSubjectId.Trim();
        ProviderEmail = providerEmail?.Trim().ToLowerInvariant();
        LinkedAt = DateTime.UtcNow;
    }

    public void RecordUsage() => LastUsedAt = DateTime.UtcNow;
}
