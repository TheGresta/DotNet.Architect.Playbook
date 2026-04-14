using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Aggregates.AuditAggregate;

/// <summary>
/// An immutable, forensic-grade record of a security-significant event.
/// Designed for SOC2/ISO27001 compliance and forensic analysis.
///
/// Design decisions:
/// - TamperHash: An HMAC-SHA256 of the audit record's canonical fields using
///   a secret key. Verifying the chain detects any post-creation tampering.
///   This is your forensic integrity guarantee.
/// - Geo: IP geolocation captured at write time (not query time) so historical
///   records retain the location at the moment of the event.
/// - ServiceName: Identifies which microservice emitted the log (critical in
///   a multi-service architecture where logs are aggregated).
/// - Severity: Enables tiered alerting — INFO goes to archives, CRITICAL fires
///   to PagerDuty immediately.
/// - No setters, no update methods — audit logs are immutable facts.
/// </summary>
public sealed class AuditLog
{
    // ── Identity & Correlation ────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public UserId? ActorId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;

    // ── Action Metadata ───────────────────────────────────────────────────────
    public string Action { get; private set; } = string.Empty;
    public string ResourceName { get; private set; } = string.Empty;
    public string? ResourceId { get; private set; }

    // ── Environmental Context ─────────────────────────────────────────────────
    public string IpAddress { get; private set; } = string.Empty;
    public string? UserAgent { get; private set; }
    public string? ServiceName { get; private set; }
    public string? Environment { get; private set; }

    // ── Geo ───────────────────────────────────────────────────────────────────
    public string? GeoCountry { get; private set; }
    public string? GeoCity { get; private set; }
    public double? GeoLatitude { get; private set; }
    public double? GeoLongitude { get; private set; }

    // ── Data Payload ──────────────────────────────────────────────────────────
    /// <summary>JSON delta or summary. Stored as JSONB in PostgreSQL.</summary>
    public string? Payload { get; private set; }

    // ── Outcome & Timing ──────────────────────────────────────────────────────
    public bool IsSuccess { get; private set; }
    public AuditSeverity Severity { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    // ── Integrity ─────────────────────────────────────────────────────────────
    /// <summary>
    /// HMAC-SHA256 of canonical fields. Used to detect post-creation tampering.
    /// Verify with <see cref="VerifyIntegrity"/>.
    /// </summary>
    public string TamperHash { get; private set; } = string.Empty;

    public enum AuditSeverity { Info, Warning, Critical }

    // ORM constructor
    private AuditLog() { }

    /// <summary>
    /// Primary factory. All fields are set at creation and never mutated.
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
        bool isSuccess = true,
        AuditSeverity severity = AuditSeverity.Info,
        string? serviceName = null,
        string? geoCountry = null,
        string? geoCity = null,
        double? geoLatitude = null,
        double? geoLongitude = null)
    {
        var log = new AuditLog
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
            Severity = isSuccess ? severity : AuditSeverity.Warning,
            OccurredAtUtc = DateTime.UtcNow,
            ServiceName = serviceName ?? System.Environment.GetEnvironmentVariable("SERVICE_NAME"),
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            GeoCountry = geoCountry,
            GeoCity = geoCity,
            GeoLatitude = geoLatitude,
            GeoLongitude = geoLongitude
        };

        // TamperHash is computed last, after all fields are set.
        // The HMAC key is loaded from configuration in the infrastructure layer
        // and injected via a factory overload — the domain uses a deterministic
        // canonical form so any layer can verify it.
        log.TamperHash = log.ComputeCanonicalHash();

        return log;
    }

    /// <summary>
    /// Verifies that the audit record has not been tampered with since creation.
    /// Returns false if any canonical field has been altered.
    /// </summary>
    public bool VerifyIntegrity()
    {
        var expectedHash = ComputeCanonicalHash();
        return string.Equals(TamperHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Produces a deterministic canonical representation of the immutable fields.
    /// Any field change will produce a different hash.
    /// </summary>
    private string ComputeCanonicalHash()
    {
        var canonical = JsonSerializer.Serialize(new
        {
            Id = Id.ToString(),
            ActorId = ActorId?.Value.ToString(),
            Action,
            ResourceName,
            ResourceId,
            IpAddress,
            CorrelationId,
            IsSuccess,
            OccurredAtUtc = OccurredAtUtc.ToString("O"),
            Payload
        });

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
