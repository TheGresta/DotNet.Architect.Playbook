using Playbook.Security.IdP.Domain.Models;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Services;

/// <summary>
/// Defines the strategy for resolving a unique, stable identity for a physical device.
/// Supports both legacy browser hashing and modern cryptographic public-key thumbprints.
/// </summary>
public interface IDeviceIdentityResolver
{
    /// <summary>
    /// Generates a normalized identity string from the provided raw device data.
    /// </summary>
    /// <param name="rawContext">The raw headers, public keys, or hardware IDs.</param>
    /// <returns>A cryptographically secure DeviceIdentity Value Object.</returns>
    DeviceIdentity Resolve(DeviceIdentityContext context);
}
