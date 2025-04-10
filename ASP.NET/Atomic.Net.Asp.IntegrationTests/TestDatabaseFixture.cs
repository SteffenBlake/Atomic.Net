using Atomic.Net.Asp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Atomic.Net.Asp.IntegrationTests;

public class TestDatabaseFixture
{
    private static string _connectionString = "";
    private static readonly object _lock = new();
    private static bool _databaseInitialized;

    public TestDatabaseFixture()
    {
        var config = new ConfigurationBuilder()
            /* .SetBasePath(Directory.GetCurrentDirectory()) */
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables("ATOMIC_ASPNET_TEST_")
            .Build();

        _connectionString = config.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException(
                "Ensure ATOMIC_ASPNET_TEST__ConnectionStrings_DefaultConnection is set"
            );

        lock (_lock)
        {
            if (!_databaseInitialized)
            {
                using (var context = CreateContext())
                {
                    context.Database.Migrate();
                }

                _databaseInitialized = true;
            }
        }
    }

    public AppDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        _ = optionsBuilder.UseNpgsql(_connectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}
