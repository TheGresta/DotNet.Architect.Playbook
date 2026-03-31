namespace Playbook.Security.IdP.Domain.ValueObjects;

public record ConsentScope
{
    public string Name { get; init; }
    public DateTime GrantedAt { get; init; }

    public ConsentScope(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Scope name cannot be empty.");
        Name = name.ToLowerInvariant().Trim();
        GrantedAt = DateTime.UtcNow;
    }
}
