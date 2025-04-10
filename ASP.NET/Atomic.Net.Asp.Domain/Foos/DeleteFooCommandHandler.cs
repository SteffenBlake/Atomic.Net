using Microsoft.EntityFrameworkCore;

namespace Atomic.Net.Asp.Domain.Foos;

public static class DeleteFooCommandHandler
{
    public static async Task<IResult<Unit>> HandleAsync(
        CommandContext<DeleteFooCommand> ctx,
        DeleteFooCommand cmd
    )
    {
        var deletions = await ctx.DB.Foos
            .Where(f => f.Id == cmd.FooId)
            .ExecuteDeleteAsync();

        if (deletions < 1)
        {
            return Result.NotFound<Unit>(nameof(FooEntity), nameof(cmd.FooId));
        }

        return Result.Success(Unit.Default);
    }
}
