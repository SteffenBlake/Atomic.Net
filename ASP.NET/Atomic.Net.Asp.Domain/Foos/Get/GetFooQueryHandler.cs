using Atomic.Net.Asp.Domain.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Atomic.Net.Asp.Domain.Foos.Get;

public static class GetFooQueryHandler
{
    public static async Task<Result<GetFooResponse>> HandleAsync(
        CommandContext<GetFooQuery> ctx,
        GetFooQuery query
    )
    {
        var result = await ctx.DB.Foos
            .Where(f => f.Id == query.FooId)
            .Select(GetFooResponse.FromEntity(ctx))
            .SingleOrDefaultAsync();

        if (result == null)
        {
            return new NotFound(nameof(FooEntity), query.FooId);
        }

        // Normally you'd just do this in the DB Query itself
        // But the point here is to demonstrate how you write
        // Unit Testable Atomic Code

        result.SensitiveData = result.SensitiveData.Sanitize();

        return result;
    }
}
