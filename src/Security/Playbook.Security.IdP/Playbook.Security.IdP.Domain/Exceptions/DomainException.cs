namespace Playbook.Security.IdP.Domain.Exceptions;

/// <summary>
/// Base exception type for all business logic and invariant violations.
/// Use this when a Domain Entity reaches an invalid state.
/// </summary>
public class DomainException : Exception
{
    public string? ErrorCode { get; }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DomainException(string message, string errorCode, Exception innerException)
       : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
