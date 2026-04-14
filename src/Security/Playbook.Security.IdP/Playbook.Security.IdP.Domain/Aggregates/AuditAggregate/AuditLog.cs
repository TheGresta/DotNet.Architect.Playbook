using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Aggregates.AuditAggregate;

/// <summary>
/// A high-assurance immutable record of a security-significant event.
/// Designed for SOC2/ISO27001 compliance and forensic analysis.
/// </summary>
public sealed class AuditLog
{
    // --- 1. Identity & Correlation ---
    public Guid Id { get; private set; }

    // The actor performing the action (null for anonymous/system actions)
    public UserId? ActorId { get; private set; }

    // Links this log to Application Logs (Grafana) and the specific HTTP request
    public string CorrelationId { get; private set; } = string.Empty;

    // --- 2. Action Metadata ---
    // Example: "User.PasswordChanged", "Admin.UserSuspended"
    public string Action { get; private set; } = string.Empty;

    // The type of resource being touched (e.g., "User", "Policy", "Client")
    public string ResourceName { get; private set; } = string.Empty;

    // The specific ID of the resource being changed
    public string? ResourceId { get; private set; }

    // --- 3. Environmental Context (The "Where") ---
    public string IpAddress { get; private set; } = string.Empty;
    public string? UserAgent { get; private set; }

    // Which environment/instance generated this log (useful for multi-region)
    public string? Environment { get; private set; }

    // --- 4. Data Payload (The "What") ---
    // Gold Standard: Stored as JSONB (Postgres) or nvarchar(max)
    // Contains "Before" and "After" snapshots or a delta.
    public string? Payload { get; private set; }

    // --- 5. Outcome & Timing ---
    public bool IsSuccess { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    // EF Core Constructor
    private AuditLog() { }

    /// <summary>
    /// Factory method for creating an immutable audit record.
    /// </summary>
    public static AuditLog Create(
        UserId? actorId,
        string action,
        string resourceName,
        string? resourceId,
        string payload,
        string ipAddress,
        string? userAgent,
        string correlationId,
        bool isSuccess = true) => new()
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = action,
            ResourceName = resourceName,
            ResourceId = resourceId,
            Payload = payload,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            IsSuccess = isSuccess,
            OccurredAtUtc = DateTime.UtcNow,
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        };
}
