namespace Playbook.Persistence.Redis.Models;

public record UserDto(Guid Id, string Email, string FullName);
