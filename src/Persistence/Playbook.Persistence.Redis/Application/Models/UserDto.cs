namespace Playbook.Persistence.Redis.Application.Models;

public record UserDto(Guid Id, string Email, string FullName) : ICacheable;
