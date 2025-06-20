using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Results;
using Atomic.Net.Asp.Domain.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace Atomic.Net.Asp.Application.CQRS.Handlers;

public static class CommandHandler
{
    public static async Task<DomainResult<TResult>> HandleAsync<TDomainContext, TRequest, TResult>(
        HttpContext httpContext,
        [FromServices] ScopedDbContext db,
        [FromServices] TDomainContext domainCtx,
        [FromServices] CommandHandler<TDomainContext, TRequest, TResult> handler,
        [AsParameters] TRequest request
    )
        where TDomainContext : class
        where TRequest : class
    {
        // We declare it as a callback to delay opening of the txn
        // Until after the other baseline Query handlers have run
        // That way we ensure the more expensive operation of opening the transaction
        // Only happens after the flight checks have passed
        async Task<DomainResult<TResult>> Curried(
            RequestContext<TDomainContext> requestCtx,
            TRequest _requestInner
        )
        {
            using var txn = await UnitOfWork.BeginWithDbTxnAsync(db);
            try
            {
                var result = await handler(txn, requestCtx, _requestInner);

                if (result.TryMatch(out TResult? _))
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
                await txn.RollbackAsync();
                throw;
            }
        }

        return await QueryHandler.HandleAsync(httpContext, db, domainCtx, Curried, request);
    }
}
