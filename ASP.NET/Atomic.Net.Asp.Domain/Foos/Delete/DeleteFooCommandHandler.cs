using Microsoft.EntityFrameworkCore;

namespace Atomic.Net.Asp.Domain.Foos.Delete;

public class DeleteFooCommandHandler(
): IRequestHandler<DeleteFooCommand, Unit>
{
    public async Task<IDomainResult<Unit>> HandleAsync(
        CommandContext ctx,
        DeleteFooCommand cmd
    )
    {
        var deletions = await ctx.DB.Foos
            .Where(f => f.Id == cmd.FooId)
            .ExecuteDeleteAsync();

        if (deletions < 1)
        {
            return new NotFound<Unit>(nameof(FooEntity), cmd.FooId);
        }

        return Unit.Default;
    }
}
