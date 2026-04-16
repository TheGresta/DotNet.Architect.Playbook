namespace Playbook.Security.IdP.Domain.ServiceModels;

/// <summary>
/// Structured result returned by IPasswordHasher.VerifyPassword.
/// Carries both the verification outcome and a rehash signal so the
/// application layer can transparently upgrade weak hashes on next login.
/// </summary>
public sealed record PasswordVerificationResult
{
    /// <summary>True when the supplied plain-text password matches the stored hash.</summary>
    public bool Verified { get; init; }

    /// <summary>
    /// True when the hash was valid but was produced with a weaker algorithm or
    /// lower work-factor than the current policy — caller should rehash and persist.
    /// Always false when <see cref="Verified"/> is false.
    /// </summary>
    public bool NeedsRehash { get; init; }

    /// <summary>Identifier of the algorithm/version that produced the stored hash (e.g. "argon2id-v3", "bcrypt-v2").</summary>
    public string? AlgorithmId { get; init; }

    // ── Static factories ──────────────────────────────────────────────────────

    /// <summary>Password matched; hash is still current — no action needed.</summary>
    public static PasswordVerificationResult Success(string? algorithmId = null) =>
        new() { Verified = true, NeedsRehash = false, AlgorithmId = algorithmId };

    /// <summary>
    /// Password matched, but the hash was produced with a legacy algorithm or
    /// insufficient work-factor.  Caller must rehash and save the new hash.
    /// </summary>
    public static PasswordVerificationResult SuccessRehashNeeded(string? algorithmId = null) =>
        new() { Verified = true, NeedsRehash = true, AlgorithmId = algorithmId };

    /// <summary>Password did not match.</summary>
    public static PasswordVerificationResult Fail() =>
        new() { Verified = false, NeedsRehash = false };
}
