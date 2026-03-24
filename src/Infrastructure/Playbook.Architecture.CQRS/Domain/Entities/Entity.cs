namespace Playbook.Architecture.CQRS.Domain.Entities;

public abstract class Entity(Guid id)
{
    public Guid Id { get; } = id;
    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;
}
