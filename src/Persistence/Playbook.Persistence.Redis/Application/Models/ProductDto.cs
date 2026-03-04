namespace Playbook.Persistence.Redis.Application.Models;

public record ProductDto(int Id, string Name, decimal Price) : ICacheable;
