using System.Net;
using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Results;
using Microsoft.AspNetCore.Mvc;
using Validly.Details;

namespace Atomic.Net.Asp.Application.CQRS.Handlers;

public static class QueryHandler 
{
    public static async Task<DomainResult<TResult>> HandleAsync<TDomainContext, TRequest, TResult>(
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

        if (result.TryMatch(out TResult? _))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
        } 
        else if (result.TryMatch(out NotFound _))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
        else if (result.TryMatch(out Conflict _))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
        }
        else if (result.TryMatch(out Unauthorized _))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else if (result.TryMatch(out ValidationResultDetails? _))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }

        return result;
    }
}
