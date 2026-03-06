namespace Playbook.Exceptions.Abstraction.Exceptions;

public abstract class DomainException(string errorCode)
    : Exception(null)
{
    public string ErrorCode { get; } = errorCode;
}