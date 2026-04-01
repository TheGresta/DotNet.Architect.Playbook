using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.Common;

/// <summary>
/// Base contract for all IDs to ensure consistent behavior across the Domain.
/// </summary>
public abstract record StronglyTypedId<T> where T : notnull
{
    public T Value { get; init; }

    protected StronglyTypedId(T value)
    {
        if (value is Guid guid && guid == Guid.Empty)
            throw new DomainException("ID cannot be an empty Guid.", "INVALID_ID");

        Value = value;
    }

    public override string ToString() => Value.ToString()!;

    public static implicit operator T(StronglyTypedId<T> id) => id.Value;
}
