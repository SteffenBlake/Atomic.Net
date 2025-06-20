using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Foos;
using Atomic.Net.Asp.Domain.Results;

namespace Atomic.Net.Asp.Application.CQRS.Handlers;

public static class ScopeEnrichmentHandler
{
    internal static async Task<DomainResult<TResult>> HandleAsync<TDomainContext, TRequest, TResult>(
        RequestContext<TDomainContext> ctx, 
        QueryHandler<TDomainContext, TRequest, TResult> handler, 
        TRequest request
    )
        where TDomainContext : class
        where TRequest : class
    {
        if (ctx.DomainContext is IFooScoped allowedFoosScope)
        {
            // Imagine if, for example, these values were pulled from something else
            // however, IE a 3rd party API or whatever
            // It'd still work fine
            allowedFoosScope.AllowedFooIds.AddRange(Enumerable.Range(1, 50));
        }

        // Handling of a variety of different scopes that can be supported
        // Including multiple on the same context
        // if (ctx.Services is IAllowedBarssScope allowedBarssScope)
        // {
        //     allowedBarssScope.AllowedBarIds.AddRange(Enumerable.Range(1, 25));
        // }

        return await handler(ctx, request);
    }
}

