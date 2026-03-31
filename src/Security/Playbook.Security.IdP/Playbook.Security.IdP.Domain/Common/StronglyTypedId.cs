namespace Playbook.Security.IdP.Domain.Common;

public abstract record StronglyTypedId<T>(T Value)
    where T : notnull
{
    public override string ToString() => Value.ToString()!;
}
