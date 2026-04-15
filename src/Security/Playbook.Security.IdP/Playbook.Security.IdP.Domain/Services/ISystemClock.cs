namespace Playbook.Security.IdP.Domain.Services;

/// <summary>
/// Domain abstraction for the current UTC time.
/// Keeps domain entities testable and free from System.DateTime coupling.
/// </summary>
public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
