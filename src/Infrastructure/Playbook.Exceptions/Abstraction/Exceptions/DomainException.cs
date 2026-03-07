using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction.Exceptions;

public abstract class DomainException(string errorCode, string? message = null, Exception? innerException = null)
    : Exception(message, innerException), IMapableException
{
    public string ErrorCode { get; } = errorCode;
    public abstract ExceptionMappingResult Map(IExceptionMapper mapper);
}
