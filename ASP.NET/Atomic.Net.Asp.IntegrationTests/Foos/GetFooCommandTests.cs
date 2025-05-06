using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Foos.Get;

namespace Atomic.Net.Asp.IntegrationTests.Foos;

public class GetFooCommandTests(TestDatabaseFixture fixture) :
    IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetFooCommand_DoesntExist_ReturnsNotFound()
    {
        // Arrange
        using var db = _fixture.CreateContext();
        using var txn = await db.Database.BeginTransactionAsync();
        var ctx = Helpers.BuildCmdContext<GetFooQuery>(db);
        var query = new GetFooQuery
        {
            FooId = 1 // Shouldnt exist
        };

        // Act
        var result = await GetFooQueryHandler.HandleAsync(ctx, query);

        // Assert
        Assert.True(result.TryMatch(out NotFound _));

        await txn.RollbackAsync();
    }

    [Fact]
    public async Task GetFooCommand_Exists_ReturnsResult()
    {
        // Arrange
        using var db = _fixture.CreateContext();
        using var txn = await db.Database.BeginTransactionAsync();
        var ctx = Helpers.BuildCmdContext<GetFooQuery>(db);

        _ = await Helpers.CreateFooAsync(
            db,
            1,
            "FIRSTNAME",
            "LASTNAME",
            "SENSITIVE"
        );
        _ = await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var query = new GetFooQuery
        {
            FooId = 1
        };

        // Act
        var result = await GetFooQueryHandler.HandleAsync(ctx, query);

        // Assert
        Assert.True(result.TryMatch(out GetFooResponse? success));
        Assert.NotNull(success);

        Assert.Equal(1, success.Id);
        Assert.Equal("FIRSTNAME LASTNAME", success.FullName);
        Assert.Equal("S****E", success.SensitiveData);

        await txn.RollbackAsync();
    }
}
