using Atomic.Net.Asp.Domain.Foos;
using Microsoft.EntityFrameworkCore;

namespace Atomic.Net.Asp.Domain;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FooEntity> Foos { get; set; } = null!;
}
