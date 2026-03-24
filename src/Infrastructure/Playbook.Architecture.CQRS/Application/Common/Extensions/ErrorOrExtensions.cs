using ErrorOr;

namespace Playbook.Architecture.CQRS.Application.Common.Extensions;

public static class ErrorOrExtensions
{
    // Maps a successful value to a new type (like our tuple) 
    // without needing to check for errors again.
    public static ErrorOr<TNext> Map<T, TNext>(
        this ErrorOr<T> result,
        Func<T, TNext> mapper)
    {
        if (result.IsError) return result.Errors;
        return mapper(result.Value);
    }

    public static async Task<ErrorOr<TNext>> MapAsync<T, TNext>(
        this Task<ErrorOr<T>> resultTask,
        Func<T, TNext> mapper)
    {
        // Wait for the previous async step (e.g., SaveProduct) to finish
        var result = await resultTask;

        // If it failed, pass the errors along
        if (result.IsError) return result.Errors;

        // If it succeeded, transform the value
        return mapper(result.Value);
    }

    public static async Task<ErrorOr<T>> EnsureFound<T>(
    this Task<T?> task,
    Error errorIfNotFound) where T : class
    {
        var result = await task;

        return result is not null
            ? result
            : errorIfNotFound;
    }

    public static async Task<ErrorOr<Success>> EnsureSuccess(
    this Task<bool> task,
    Error errorIfFailed)
    {
        var success = await task;
        return success ? Result.Success : errorIfFailed;
    }
}
