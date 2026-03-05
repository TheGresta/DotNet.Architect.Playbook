namespace Playbook.Exceptions.Domain;

public abstract class DomainException(string message, string errorCode)
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
