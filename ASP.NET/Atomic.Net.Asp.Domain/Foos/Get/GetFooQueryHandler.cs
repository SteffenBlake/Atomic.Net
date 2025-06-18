using Atomic.Net.Asp.Domain.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Atomic.Net.Asp.Domain.Foos.Get;

public static class GetFooQueryHandler
{
    public record GetFooContext(
        DomainScope AllowedFooIds
    ) : IFooScoped;

    public static readonly QueryHandler<GetFooContext, GetFooQuery, GetFooResponse> Run =
        async (ctx, query) => 
    {
        var result = await ctx.DB.FoosScoped(ctx.DomainContext)
            .Where(f => f.Id == query.FooId)
            .Select(GetFooResponse.FromEntity(ctx.DB))
            .SingleOrDefaultAsync();

        if (result == null)
        {
            return new NotFound<GetFooResponse>(nameof(FooEntity), query.FooId);
        }

        // Normally you'd just do this in the DB Query itself
        // But the point here is to demonstrate how you write
        // Unit Testable Atomic Code

        result.SensitiveData = result.SensitiveData.Sanitize();

        return result;
    };
}
