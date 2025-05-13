using System.Diagnostics;
using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Foos;
using Microsoft.EntityFrameworkCore;

namespace Atomic.Net.Asp.DataService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    DataServiceConfiguration config
) : BackgroundService
{
    public const string ActivitySourceName = "DataService";
    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await MigrateDatabaseAsync(db, cancellationToken);
        if (config.SeedData)
        {
            await SeedDataAsync(db, cancellationToken);
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task MigrateDatabaseAsync(
        AppDbContext db, CancellationToken cancellationToken
    )
    {
        using var activity = _activitySource.StartActivity(
            "Data Migration", ActivityKind.Client
        );

        try
        {
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
                if (db.Database.HasPendingModelChanges())
                {
                    await db.Database.MigrateAsync(cancellationToken);
                }
            });
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }
    }

    private static async Task SeedDataAsync(
        AppDbContext db, CancellationToken cancellationToken
    )
    {

        using var activity = _activitySource.StartActivity(
            "Data Seeding", ActivityKind.Client
        );

        try
        {
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                _ = await EnsureFoosAsync(db, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }
    }

    private static async Task<List<FooEntity>> EnsureFoosAsync(
        AppDbContext db, CancellationToken cancellationToken
    )
    {
        var existing = await db.Foos.AnyAsync(cancellationToken);
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
            _ = await db.Foos.AddAsync(foo, cancellationToken);
        }
        _ = await db.SaveChangesAsync(cancellationToken);
        return foos;
    }
}
