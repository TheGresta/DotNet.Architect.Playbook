using System.Text.RegularExpressions;

using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

/// <summary>
/// E.164-normalised phone number value object.
/// E.164 format: +[country code][subscriber number] e.g. +14155552671
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    private static readonly Regex E164Pattern =
        new(@"^\+[1-9]\d{6,14}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public string Value { get; }

    private PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Phone number cannot be empty.", "INVALID_PHONE");

        var normalized = value.Trim();

        if (!E164Pattern.IsMatch(normalized))
            throw new DomainException(
                "Phone number must be in E.164 format (e.g., +14155552671).",
                "INVALID_PHONE_FORMAT");

        Value = normalized;
    }

    public static PhoneNumber Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PhoneNumber phone) => phone.Value;
}
