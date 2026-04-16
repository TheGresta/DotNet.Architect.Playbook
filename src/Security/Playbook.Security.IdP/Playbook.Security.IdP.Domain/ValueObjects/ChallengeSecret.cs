using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

public sealed class ChallengeSecret : ValueObject
{
    public string Value { get; }

    private ChallengeSecret(string value)
    {
        // 64 chars because 32 bytes in Hex = 64 characters
        if (string.IsNullOrWhiteSpace(value) || value.Length < 64)
            throw new DomainException("Challenge secret must be a high-entropy string (min 64 chars).", "WEAK_CHALLENGE_SECRET");

        Value = value;
    }

    public static ChallengeSecret Generate()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return new ChallengeSecret(Convert.ToHexString(bytes));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(ChallengeSecret secret) => secret.Value;
}
