using System.Net;
using Atomic.Net.Asp.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Atomic.Net.Asp.Application.CQRS.Handlers;

public static class QueryHandler 
{
    public static async Task<IDomainResult<TResult>> HandleAsync<TResult, TServices, TRequest>(
        HttpContext httpContext, 
        [FromServices]AppDbContext db,
        [FromServices] TServices services,
        [FromServices] RequestHandler<TResult, TServices, TRequest> handler,
        [AsParameters]TRequest request
    )
        where TServices : class
        where TRequest : class
    {
        var userId = httpContext.User.Identity?.Name;
        var ctx = new RequestContext<TServices>(services, userId, db);

        var result = await ValidationHandler.HandleAsync(ctx, handler, request);

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
