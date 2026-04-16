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

    // ── Chain Integrity ───────────────────────────────────────────────────────
    /// <summary>
    /// HMAC-SHA256 of the previous record in the audit chain, or
    /// <c>"CHAIN_ORIGIN"</c> for the very first record.
    /// Including this in the current record's hash binds the records into a
    /// tamper-evident chain: deletion or reordering of any record breaks the
    /// chain.
    /// </summary>
    public string PreviousHash { get; private set; } = "CHAIN_ORIGIN";

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
    /// <param name="hmacKey">
    /// Secret key for HMAC-SHA256, sourced from secure configuration
    /// (e.g. Azure Key Vault / AWS Secrets Manager). Must not be stored in the
    /// domain — pass it in from the infrastructure/application layer.
    /// </param>
    /// <param name="previousHash">
    /// The <see cref="TamperHash"/> of the immediately preceding audit record
    /// for this tenant/service, forming a tamper-evident chain.
    /// Pass <c>null</c> (or omit) for the very first record.
    /// </param>
    public static AuditLog Create(
        UserId? actorId,
        string action,
        string resourceName,
        string? resourceId,
        string payload,
        string ipAddress,
        string? userAgent,
        string correlationId,
        byte[] hmacKey,
        string serviceName,
        string environment,
        bool isSuccess = true,
        AuditSeverity severity = AuditSeverity.Info,
        string? previousHash = null,
        DateTimeOffset? occurredAtUtc = null,
        string? geoCountry = null,
        string? geoCity = null,
        double? geoLatitude = null,
        double? geoLongitude = null)
    {
        ArgumentNullException.ThrowIfNull(hmacKey);
        if (hmacKey.Length == 0)
            throw new ArgumentException("HMAC key must not be empty.", nameof(hmacKey));

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
            Severity = !isSuccess && severity == AuditSeverity.Info
                ? AuditSeverity.Warning
                : severity,
            OccurredAtUtc = occurredAtUtc?.UtcDateTime ?? DateTime.UtcNow,
            ServiceName = serviceName,
            Environment = environment,
            GeoCountry = geoCountry,
            GeoCity = geoCity,
            GeoLatitude = geoLatitude,
            GeoLongitude = geoLongitude,
            PreviousHash = previousHash ?? "CHAIN_ORIGIN"
        };

        // TamperHash is computed last, after all fields are set.
        log.TamperHash = log.ComputeHmac(hmacKey);

        return log;
    }

    /// <summary>
    /// Verifies that this record has not been tampered with since creation.
    /// Returns <c>false</c> if any canonical field has been altered.
    /// </summary>
    /// <param name="hmacKey">
    /// The same secret key that was used during <see cref="Create"/>.
    /// </param>
    public bool VerifyIntegrity(byte[] hmacKey)
    {
        ArgumentNullException.ThrowIfNull(hmacKey);
        var expectedHash = ComputeHmac(hmacKey);
        return string.Equals(TamperHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }


    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Produces a deterministic canonical representation of ALL immutable fields
    /// (including <see cref="PreviousHash"/> and geo metadata) and signs it with
    /// HMAC-SHA256 using the supplied key.
    /// Any field change — even in previously omitted fields — will produce a
    /// different hash.
    /// </summary>
    private string ComputeHmac(byte[] key)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            Id = Id.ToString(),
            ActorId = ActorId?.Value.ToString(),
            Action,
            ResourceName,
            ResourceId,
            IpAddress,
            UserAgent,
            CorrelationId,
            IsSuccess,
            Severity = Severity.ToString(),
            OccurredAtUtc = OccurredAtUtc.ToString("O"),
            Payload,
            ServiceName,
            Environment,
            GeoCountry,
            GeoCity,
            GeoLatitude,
            GeoLongitude,
            PreviousHash
        });

        var data = Encoding.UTF8.GetBytes(canonical);
        var hmacBytes = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hmacBytes).ToLowerInvariant();
    }
}
