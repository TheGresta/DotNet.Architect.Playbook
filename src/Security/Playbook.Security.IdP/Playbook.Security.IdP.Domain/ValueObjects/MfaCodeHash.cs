using System;
using System.Collections.Generic;
using System.Text;

using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

public sealed class MfaCodeHash : ValueObject
{
    public string Value { get; }

    private MfaCodeHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("MFA hash cannot be empty.");

        Value = value;
    }

    public static MfaCodeHash FromString(string hash) => new(hash);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
