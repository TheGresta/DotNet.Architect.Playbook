using ErrorOr;

namespace Playbook.Architecture.CQRS.Application.Common.Extensions;

/// <summary>
/// Provides a suite of high-level extension methods for <see cref="ErrorOr{TValue}"/> to facilitate 
/// functional programming patterns, such as Monadic mapping and asynchronous flow control.
/// These methods reduce boilerplate by abstracting error checking and task awaiting.
/// </summary>
public static class ErrorOrExtensions
{
    /// <summary>
    /// Synchronously transforms the underlying value of a successful <see cref="ErrorOr{T}"/> into a new type.
    /// If the result contains errors, the transformation is bypassed and the errors are propagated.
    /// </summary>
    /// <typeparam name="T">The type of the current value.</typeparam>
    /// <typeparam name="TNext">The type of the resulting value.</typeparam>
    /// <param name="result">The source <see cref="ErrorOr{T}"/> instance.</param>
    /// <param name="mapper">The delegate defining the transformation logic.</param>
    /// <returns>A new <see cref="ErrorOr{TNext}"/> containing either the mapped value or the original errors.</returns>
    public static ErrorOr<TNext> Map<T, TNext>(
        this ErrorOr<T> result,
        Func<T, TNext> mapper) => result.IsError ? result.Errors : mapper(result.Value);

    /// <summary>
    /// Asynchronously awaits an <see cref="ErrorOr{T}"/> task and maps its successful value to a new type.
    /// This enables seamless chaining of asynchronous operations in a functional style.
    /// </summary>
    /// <typeparam name="T">The type of the current value.</typeparam>
    /// <typeparam name="TNext">The type of the resulting value.</typeparam>
    /// <param name="resultTask">A task representing the asynchronous <see cref="ErrorOr{T}"/> result.</param>
    /// <param name="mapper">The delegate defining the transformation logic.</param>
    /// <returns>A task representing the asynchronous <see cref="ErrorOr{TNext}"/> result.</returns>
    public static async Task<ErrorOr<TNext>> MapAsync<T, TNext>(
        this Task<ErrorOr<T>> resultTask,
        Func<T, TNext> mapper)
    {
        // Await the task once to access the inner ErrorOr result before applying mapping logic.
        var result = await resultTask;
        return result.IsError ? result.Errors : mapper(result.Value);
    }

    /// <summary>
    /// Elevates a nullable reference type task into the <see cref="ErrorOr{T}"/> domain.
    /// It validates the existence of the object, returning the provided error if the result is null.
    /// </summary>
    /// <typeparam name="T">The type of the reference object, constrained to classes.</typeparam>
    /// <param name="task">The asynchronous operation returning a nullable object.</param>
    /// <param name="errorIfNotFound">The specific <see cref="Error"/> to return if the task yields null.</param>
    /// <returns>An <see cref="ErrorOr{T}"/> containing the non-null object or a "Not Found" style error.</returns>
    public static async Task<ErrorOr<T>> EnsureFound<T>(
        this Task<T?> task,
        Error errorIfNotFound) where T : class
    {
        // Pattern match against the awaited result to handle nullability in a single expression.
        return await task is T result ? result : errorIfNotFound;
    }

    /// <summary>
    /// Converts a boolean-based asynchronous operation into a formal <see cref="ErrorOr{Success}"/> result.
    /// Useful for bridging legacy boolean-return methods with modern functional error handling.
    /// </summary>
    /// <param name="task">The asynchronous operation returning a boolean success flag.</param>
    /// <param name="errorIfFailed">The specific <see cref="Error"/> to return if the task yields false.</param>
    /// <returns>A successful <see cref="Result.Success"/> if true; otherwise, the specified error.</returns>
    public static async Task<ErrorOr<Success>> EnsureSuccess(
        this Task<bool> task,
        Error errorIfFailed) => await task ? Result.Success : errorIfFailed;
}
