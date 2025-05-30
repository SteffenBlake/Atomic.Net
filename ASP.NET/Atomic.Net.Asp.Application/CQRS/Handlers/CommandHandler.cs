using Atomic.Net.Asp.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Atomic.Net.Asp.Application.CQRS.Handlers;

public static class CommandHandler
{
    public static async Task<IDomainResult<TResult>> HandleAsync<TResult, TServices, TRequest>(
        HttpContext httpContext,
        [FromServices] AppDbContext db,
        [FromServices] TServices services,
        [FromServices] RequestHandler<TResult, TServices, TRequest> handler,
        [AsParameters] TRequest request
    )
        where TServices : class
        where TRequest : class
    {
        using var txn = await db.Database.BeginTransactionAsync();
        try
        {
            var result = await QueryHandler.HandleAsync(
                httpContext, db, services, handler, request
            );
            if (result is TResult)
            {
                await txn.CommitAsync();
            }
            else
            {
                await txn.RollbackAsync();
            }
            return result;
        }
        catch (Exception)
        {
            txn.Rollback();
            throw;
        }
    }
}
