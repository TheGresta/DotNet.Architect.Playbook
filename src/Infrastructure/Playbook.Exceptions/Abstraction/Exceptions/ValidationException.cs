using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction.Exceptions;

/// <summary>
/// Represents an exception containing a collection of validation failures, 
/// typically originating from input or command validation layers.
/// </summary>
/// <param name="errors">A dictionary where keys are property names and values are arrays of associated validation errors.</param>
public sealed class ValidationException(IReadOnlyDictionary<string, ValidationError[]> errors)
    : DomainException(ErrorCodes.ValidationError)
{
    /// <summary>
    /// Gets the read-only collection of validation errors grouped by member name.
    /// </summary>
    public IReadOnlyDictionary<string, ValidationError[]> Errors { get; } = errors;

    /// <summary>
    /// Dispatches the exception to the appropriate mapping logic using the Visitor pattern.
    /// </summary>
    /// <param name="mapper">The implementation of <see cref="IExceptionMapper"/>.</param>
    /// <returns>The result of the mapping operation.</returns>
    public override ExceptionMappingResult Map(IExceptionMapper mapper)
        => mapper.MapSpecific(this);
}
