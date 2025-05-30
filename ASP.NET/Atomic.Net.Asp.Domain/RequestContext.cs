namespace Atomic.Net.Asp.Domain;

public record RequestContext<TServices>
(
    TServices Services,
    string? UserId,
    AppDbContext DB
)
where TServices : class;
