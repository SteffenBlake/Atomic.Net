using Atomic.Net.Asp.Domain.Foos;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Atomic.Net.Asp.Domain;

public class ScopedDbContext(AppDbContext db)
{
    public IQueryable<FooEntity> FoosScoped(IFooScoped scope) => 
        db.Foos.Where(f => scope.AllowedFooIds.Contains(f.Id));

    public DatabaseFacade Database => db.Database;
}

