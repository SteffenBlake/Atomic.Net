namespace Atomic.Net.Asp.Domain;

public record CommandContext(
    AppDbContext DB, 
    string? UserId
);
