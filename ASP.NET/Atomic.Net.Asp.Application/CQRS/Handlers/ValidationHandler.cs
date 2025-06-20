using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Extensions;
using Atomic.Net.Asp.Domain.Results;
using Atomic.Net.Asp.Domain.Validation;

namespace Atomic.Net.Asp.Application.CQRS.Handlers;

public static class ValidationHandler 
{
    public static async Task<DomainResult<TResult>> HandleAsync<TDomainContext, TRequest, TResult>(
        RequestContext<TDomainContext> ctx,
        QueryHandler<TDomainContext, TRequest, TResult> handler,
        TRequest request
    )
        where TDomainContext : class
        where TRequest : class
    {
        if (request is IDomainValidatable validatable)
        {
            var validationResult = validatable.Validate();
            if (!validationResult.IsSuccess)
            {
                return validationResult.GetProblemDetails();
            }
        }
            
        return await ScopeEnrichmentHandler.HandleAsync(ctx, handler, request);
    }
}
