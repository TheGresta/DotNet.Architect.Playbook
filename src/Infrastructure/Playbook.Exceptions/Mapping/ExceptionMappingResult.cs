namespace Playbook.Exceptions.Mapping;

public record ExceptionMappingResult(
int StatusCode,
string Title,
string Detail,
string ErrorCode,
IEnumerable<KeyValuePair<string, string[]>>? ValidationErrors);
