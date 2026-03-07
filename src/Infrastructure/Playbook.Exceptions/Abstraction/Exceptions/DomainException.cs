using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction.Exceptions;

/// <summary>
/// Serves as the base class for all domain-specific exceptions within the application.
/// This abstract class enforces a standardized <see cref="ErrorCode"/> and implements
/// <see cref="IMapableException"/> to support the Visitor-based mapping pattern.
/// </summary>
/// <param name="errorCode">A unique, machine-readable string identifying the specific error type.</param>
/// <param name="message">A human-readable explanation of the exception. Defaults to null.</param>
/// <param name="innerException">The exception that is the cause of the current exception, if applicable.</param>
public abstract class DomainException(string errorCode, string? message = null, Exception? innerException = null)
    : Exception(message, innerException), IMapableException
{
    /// <summary>
    /// Gets the unique error code associated with this exception instance.
    /// This code is typically used by client applications for programmatic error handling.
    /// </summary>
    public string ErrorCode { get; } = errorCode;

    /// <summary>
    /// When implemented in a derived class, dispatches the exception to the appropriate 
    /// specialized mapping method on the <see cref="IExceptionMapper"/>.
    /// </summary>
    /// <param name="mapper">The mapper instance responsible for the double-dispatch transformation.</param>
    /// <returns>An <see cref="ExceptionMappingResult"/> representing the mapped error state.</returns>
    public abstract ExceptionMappingResult Map(IExceptionMapper mapper);
}
