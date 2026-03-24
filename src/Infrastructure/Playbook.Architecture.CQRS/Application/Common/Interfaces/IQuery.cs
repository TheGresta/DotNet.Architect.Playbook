using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Interfaces;

/// <summary>
/// Defines a specialized contract for Read-only operations within the application.
/// Queries are architecturally intended to be side-effect free and are primary candidates for caching strategies.
/// By inheriting from <see cref="IRequest{TResponse}"/>, these queries integrate directly with the MediatR pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of the response, constrained to <see cref="IErrorOr"/> to ensure structured failure handling.</typeparam>
public interface IQuery<TResponse> : IRequest<TResponse> where TResponse : IErrorOr { }
