
using Atomic.Net.Asp.Domain.Foos;
using Microsoft.EntityFrameworkCore;

namespace Atomic.Net.Asp.Domain;

public static class DbInitializer
{
    public static async Task RunAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        var txn = await db.Database.BeginTransactionAsync();
        try
        {
            _ = await EnsureFoosAsync(db);
        }
        catch (Exception)
        {
            txn.Rollback();
            throw;
        }

        await txn.CommitAsync();
    }

    private static async Task<List<FooEntity>> EnsureFoosAsync(AppDbContext db)
    {
        var existing = await db.Foos.AnyAsync();
        if (existing)
        {
            return [];
        }

        var foos = new List<FooEntity>();
        for (var n = 1; n <= 100; n++)
        {
            var foo = new FooEntity()
            {
                FirstName = $"FIRSTNAME_{n}",
                LastName= $"LASTNAME_{n}",
                SensitiveData = Guid.NewGuid().ToString()
            };
            foos.Add(foo);
            _ = db.Foos.Add(foo);
        }
        _ = await db.SaveChangesAsync();
        return foos;
    }
}
