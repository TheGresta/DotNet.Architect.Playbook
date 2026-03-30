using ProtoBuf;

namespace Playbook.Persistence.HybridCaching.Core.Entities;

// The "Smart" Record
[ProtoContract]
public record Product
{
    [ProtoMember(1)] public int Id { get; init; }
    [ProtoMember(2)] public string Name { get; init; } = string.Empty;
    [ProtoMember(3)] public decimal Price { get; init; }
    [ProtoMember(4)] public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    [ProtoMember(5)] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
