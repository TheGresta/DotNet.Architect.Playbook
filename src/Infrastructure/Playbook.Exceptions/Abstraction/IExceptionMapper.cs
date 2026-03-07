using Playbook.Exceptions.Abstraction.Exceptions;
using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction;

/// <summary>
/// Defines the Visitor interface for mapping domain-specific exceptions into specialized result types.
/// </summary>
public interface IExceptionMapper
{
    /// <summary>
    /// Maps a <see cref="Exception"/> to an <see cref="ExceptionMappingResult"/>.
    /// </summary>
    ExceptionMappingResult Map(Exception ex);
    /// <summary>
    /// Maps a <see cref="NotFoundException"/> to an <see cref="ExceptionMappingResult"/>.
    /// </summary>
    ExceptionMappingResult MapSpecific(NotFoundException ex);

    /// <summary>
    /// Maps a <see cref="BusinessRuleException"/> to an <see cref="ExceptionMappingResult"/>.
    /// </summary>
    ExceptionMappingResult MapSpecific(BusinessRuleException ex);

    /// <summary>
    /// Maps a <see cref="ValidationException"/> to an <see cref="ExceptionMappingResult"/>.
    /// </summary>
    ExceptionMappingResult MapSpecific(ValidationException ex);

    /// <summary>
    /// Provides a fallback mapping mechanism for unhandled or generic exceptions.
    /// </summary>
    ExceptionMappingResult MapFallback(Exception ex);
}
