using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

public sealed class ConsentScope : ValueObject
{
    public string Name { get; }
    public DateTime GrantedAt { get; }

    public ConsentScope(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Scope name cannot be empty.");

        Name = name.ToLowerInvariant().Trim();
        GrantedAt = DateTime.UtcNow;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        // Two scopes are equal if they represent the same permission, 
        // regardless of when they were granted.
        yield return Name;
    }
}
