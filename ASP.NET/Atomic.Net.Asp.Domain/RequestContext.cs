namespace Atomic.Net.Asp.Domain;

public record RequestContext<TDomainContext>
(
    TDomainContext DomainContext,
    string? UserId,
    ScopedDbContext DB
)
where TDomainContext : class;
