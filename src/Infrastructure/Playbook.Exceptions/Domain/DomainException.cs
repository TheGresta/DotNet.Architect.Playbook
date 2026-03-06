namespace Playbook.Exceptions.Domain;

public abstract class DomainException(string errorCode)
    : Exception(null)
{
    public string ErrorCode { get; } = errorCode;
}