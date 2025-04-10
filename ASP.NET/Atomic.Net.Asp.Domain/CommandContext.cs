using Microsoft.Extensions.Logging;

namespace Atomic.Net.Asp.Domain;

public class CommandContext<T>(
    ILogger<T> logger, AppDbContext db, string? userId
)
{
    public ILogger<T> Logger => logger;
    public AppDbContext DB => db;
    public string? UserId => userId;
}
