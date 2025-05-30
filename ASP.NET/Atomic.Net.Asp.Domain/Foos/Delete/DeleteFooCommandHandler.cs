using Microsoft.EntityFrameworkCore;

namespace Atomic.Net.Asp.Domain.Foos.Delete;

public static class DeleteFooCommandHandler
{
    public static readonly RequestHandler<Unit, Unit, DeleteFooCommand> Run = 
        async (ctx, cmd ) =>
    {
        var deletions = await ctx.DB.Foos
            .Where(f => f.Id == cmd.FooId)
            .ExecuteDeleteAsync();

        if (deletions < 1)
        {
            return new NotFound<Unit>(nameof(FooEntity), cmd.FooId);
        }

        return Unit.Default;
    };
}
