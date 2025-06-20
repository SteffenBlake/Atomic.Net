using Atomic.Net.Asp.Domain.Results;
using Atomic.Net.Asp.Domain.ThirdParty;
using Microsoft.EntityFrameworkCore;

namespace Atomic.Net.Asp.Domain.Foos.Delete;

public static class DeleteFooCommandHandler
{
    public record DeleteFooContext(
        ThirdPartyHttpClient ThirdPartyClient,
        DomainScope AllowedFooIds
    ) : IFooScoped;

    public static readonly CommandHandler<DeleteFooContext, DeleteFooCommand, Unit> Run = 
        async (txn, ctx, cmd ) =>
    {
        var deletions = await ctx.DB.FoosScoped(ctx.DomainContext)
            .Where(f => f.Id == cmd.FooId)
            .ExecuteDeleteAsync();

        if (deletions < 1)
        {
            return new NotFound(nameof(FooEntity), cmd.FooId);
        }

        return await ctx.DomainContext.ThirdPartyClient
            .ThirdPartyNotifyFooDeletedAsync(txn, cmd.FooId);
    };
}
