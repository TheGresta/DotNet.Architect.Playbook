using ErrorOr;

namespace Playbook.Architecture.CQRS.Application.Common.Extensions;

public static class ErrorOrExtensions
{
    /// <summary>
    /// Synchronously maps a successful value to a new type.
    /// </summary>
    public static ErrorOr<TNext> Map<T, TNext>(
        this ErrorOr<T> result,
        Func<T, TNext> mapper) => result.IsError ? result.Errors : mapper(result.Value);

    /// <summary>
    /// Asynchronously maps a task-wrapped ErrorOr result to a new type.
    /// </summary>
    public static async Task<ErrorOr<TNext>> MapAsync<T, TNext>(
        this Task<ErrorOr<T>> resultTask,
        Func<T, TNext> mapper)
    {
        var result = await resultTask;
        return result.IsError ? result.Errors : mapper(result.Value);
    }

    /// <summary>
    /// Converts a nullable reference type task into an ErrorOr result.
    /// </summary>
    public static async Task<ErrorOr<T>> EnsureFound<T>(
        this Task<T?> task,
        Error errorIfNotFound) where T : class => await task is T result ? result : errorIfNotFound;

    /// <summary>
    /// Converts a boolean task into a Success or a specific Error.
    /// </summary>
    public static async Task<ErrorOr<Success>> EnsureSuccess(
        this Task<bool> task,
        Error errorIfFailed) => await task ? Result.Success : errorIfFailed;
}
