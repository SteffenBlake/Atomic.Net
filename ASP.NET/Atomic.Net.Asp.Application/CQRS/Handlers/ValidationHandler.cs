using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Extensions;
using Atomic.Net.Asp.Domain.Validation;

namespace Atomic.Net.Asp.Application.CQRS.Handlers;

public static class ValidationHandler 
{
    public static async Task<IDomainResult<TResult>> HandleAsync<TDomainContext, TRequest, TResult>(
        RequestContext<TDomainContext> ctx,
        QueryHandler<TDomainContext, TRequest, TResult> handler,
        TRequest request
    )
        where TDomainContext : class
        where TRequest : class
    {
        if (
            request is IDomainValidatable validatable && 
            validatable.HasErrors<TResult>(out var result)
        )
        {
            return result; 
        }

        return await ScopeEnrichmentHandler.HandleAsync(ctx, handler, request);
    }
}
