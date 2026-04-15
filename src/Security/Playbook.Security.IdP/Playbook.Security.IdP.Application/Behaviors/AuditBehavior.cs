using ErrorOr;

using MediatR;

using Microsoft.Extensions.Logging;

using Playbook.Security.IdP.Application.Abstractions.Data;
using Playbook.Security.IdP.Application.Abstractions.Messaging;
using Playbook.Security.IdP.Application.Abstractions.Security;
using Playbook.Security.IdP.Domain.Aggregates.AuditAggregate;

namespace Playbook.Security.IdP.Application.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditRepository auditRepository,
    IRequestContext requestContext,
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IAuditableRequest
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. Execute the Primary Business Logic
        // We wait for the response so we can audit the outcome (Success/Failure)
        var response = await next();

        try
        {
            // 2. Build the Immutable Audit Record
            // We pull data from the IAuditableRequest (The "What") 
            // and the IRequestContext (The "Who/Where")
            var auditLog = AuditLog.Create(
                actorId: requestContext.UserId,
                action: typeof(TRequest).Name,
                resourceName: request.ResourceName,
                resourceId: request.ResourceId,
                serviceName: string.Empty,
                environment: string.Empty,
                hmacKey: [],
                payload: request.GetAuditSummary(),
                ipAddress: requestContext.IpAddress,
                userAgent: requestContext.UserAgent,
                correlationId: requestContext.CorrelationId,
                isSuccess: !response.IsError
            );

            // 3. Persist to the Audit Store
            // This is handled within the same Unit of Work as the main request
            // to ensure the action and the audit log are atomically committed.
            await auditRepository.AddAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            // 4. Critical Failure Handling
            // If auditing fails, we log a Critical Error to Grafana/Loki.
            logger.LogCritical(ex,
                "AUDIT FAILURE: Could not persist audit log for {RequestName}. CorrelationId: {CorrelationId}",
                typeof(TRequest).Name,
                requestContext.CorrelationId);

            throw;
        }

        return response;
    }
}
