using Playbook.Security.IdP.Domain.ServiceModels;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Services;

public interface IPasswordHasher
{
    /// <summary>
    /// Returns a strongly-typed PasswordHash Value Object.
    /// </summary>
    PasswordHash HashPassword(string password);

    /// <summary>
    /// Verifies <paramref name="password"/> against <paramref name="storedHash"/>.
    /// Returns a <see cref="PasswordVerificationResult"/> that signals both the
    /// outcome and whether a transparent rehash should be performed.
    /// </summary>
    PasswordVerificationResult VerifyPassword(string password, PasswordHash storedHash);
}
