namespace Playbook.Persistence.HybridCaching.Core.Entities;

// The "Casual" Record (No attributes, standard POCO)
public record CasualWayProduct
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
