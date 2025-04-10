using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Foos;
using Microsoft.Extensions.Logging.Abstractions;

namespace Atomic.Net.Asp.IntegrationTests;

public static class Helpers
{
    public const string USERID = nameof(USERID);

    public static CommandContext<TRequest> BuildCmdContext<TRequest>(
        AppDbContext db
    )
        where TRequest : class
    {
        return new CommandContext<TRequest>(
           NullLogger<TRequest>.Instance, db, USERID
        );
    }

    public static async Task<FooEntity> CreateFooAsync(
        AppDbContext db,
        int id = 1,
        string firstName = "FIRSTNAME",
        string lastName = "LASTNAME",
        string sensitiveData = "SENSITIVE"
    )
    {
        var foo = new FooEntity()
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            SensitiveData = sensitiveData
        };

        _ = await db.Foos.AddAsync(foo);

        return foo;
    }
}
