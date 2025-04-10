using Atomic.Net.Asp.Domain;

namespace Atomic.Net.Asp.Application;

public class WebContext<T>(
    ILogger<T> logger, AppDbContext db
)
{
    public ILogger<T> Logger => logger;
    public AppDbContext DB => db;

    public CommandContext<T> Compile(HttpContext ctx)
    {
        var userId = ctx.User.Identity?.Name;
        return new CommandContext<T>(logger, db, userId);
    }
}
