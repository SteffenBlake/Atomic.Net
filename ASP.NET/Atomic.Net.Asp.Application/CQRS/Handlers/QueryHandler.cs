using System.Net;
using Atomic.Net.Asp.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Atomic.Net.Asp.Application.CQRS.Handlers;

public static class QueryHandler 
{
    public static async Task<IDomainResult<TResult>> HandleAsync<TDomainContext, TRequest, TResult>(
        HttpContext httpContext, 
        [FromServices]ScopedDbContext db,
        [FromServices] TDomainContext domainCtx,
        [FromServices] QueryHandler<TDomainContext, TRequest, TResult> handler,
        [AsParameters]TRequest request
    )
        where TDomainContext : class
        where TRequest : class
    {
        var userId = httpContext.User.Identity?.Name;
        var requestCtx = new RequestContext<TDomainContext>(domainCtx, userId, db);

        var result = await ValidationHandler.HandleAsync(requestCtx, handler, request);

        httpContext.Response.StatusCode = result switch
        {
            TResult => (int)HttpStatusCode.OK,
            ValidationDetails<TResult> => (int)HttpStatusCode.BadRequest,
            NotFound<TResult> => (int)HttpStatusCode.NotFound,
            Conflict<TResult> => (int)HttpStatusCode.Conflict,
            Unauthorized<TResult> => (int)HttpStatusCode.Unauthorized,
            _ => throw new ArgumentOutOfRangeException(nameof(result))
        };

        return result;
    }
}
