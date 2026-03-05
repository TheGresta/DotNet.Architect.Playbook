namespace Playbook.Exceptions.Mapping;

public record ExceptionMappingResult(
int StatusCode,
string Title,
string Detail,
string ErrorCode,
IDictionary<string, string[]>? ValidationErrors);
