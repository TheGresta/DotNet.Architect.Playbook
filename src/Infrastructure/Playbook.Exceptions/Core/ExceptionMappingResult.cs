namespace Playbook.Exceptions.Core;

public record ExceptionMappingResult(
int StatusCode,
string Title,
string Detail,
string ErrorCode,
IEnumerable<KeyValuePair<string, string[]>>? ValidationErrors);
