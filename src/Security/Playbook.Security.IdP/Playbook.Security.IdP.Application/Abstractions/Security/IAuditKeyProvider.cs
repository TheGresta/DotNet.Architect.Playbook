namespace Playbook.Security.IdP.Application.Abstractions.Security;

public interface IAuditKeyProvider
{
    /// <summary>Returns the raw HMAC key bytes. Never returns null or empty.</summary>
    ValueTask<byte[]> GetKeyAsync(CancellationToken ct = default);
}
