using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction;

/// <summary>
/// Defines a contract for exceptions that support polymorphic mapping to external error formats.
/// Implements the Double Dispatch mechanism of the Visitor pattern.
/// </summary>
public interface IMappableException
{
    /// <summary>
    /// Maps the current exception instance using the provided mapper.
    /// </summary>
    /// <param name="mapper">The visitor responsible for transforming the exception.</param>
    /// <returns>The mapped representation of the error.</returns>
    ExceptionMappingResult Map(IExceptionMapper mapper);
}
