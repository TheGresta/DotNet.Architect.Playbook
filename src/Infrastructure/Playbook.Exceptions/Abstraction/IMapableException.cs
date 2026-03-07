using Playbook.Exceptions.Core;

namespace Playbook.Exceptions.Abstraction;

public interface IMapableException
{
    ExceptionMappingResult Map(IExceptionMapper mapper);
}
