namespace Playbook.Exceptions.Mapping;

public interface IExceptionMapper
{
    // Determines if this mapper can handle the specific exception
    bool CanMap(Exception exception);

    // Performs the mapping logic
    ExceptionMappingResult Map(Exception exception);
}
