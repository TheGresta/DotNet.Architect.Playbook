using Playbook.Exceptions.Abstraction.Exceptions;
using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction;

public interface IExceptionMapper
{
    ExceptionMappingResult MapSpecific(NotFoundException ex);
    ExceptionMappingResult MapSpecific(BusinessRuleException ex);
    ExceptionMappingResult MapSpecific(ValidationException ex);
    ExceptionMappingResult MapFallback(Exception ex);
}
