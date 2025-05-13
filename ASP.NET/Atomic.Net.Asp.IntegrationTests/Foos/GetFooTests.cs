using System.Net.Http.Json;
using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Foos.Get;
using Microsoft.AspNetCore.Http.HttpResults;
using Validly.Details;

namespace Atomic.Net.Asp.IntegrationTests.Foos;

[Collection("Global Test Collection")]
public class GetFooTests(AppHostFixture appHostFixture)
{
    private readonly AppHostFixture _appHostFixture = appHostFixture;    

    [Fact]
    public async Task GetFoo_Exists_ReturnsValidData()
    {
        // Act
        var response = await _appHostFixture.AppHttpClient.GetAsync("/foos/1");
        var result = await response.Content.ReadFromJsonAsync<GetFooResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("FIRSTNAME_1 LASTNAME_1", result.FullName);
    }

    [Fact]
    public async Task GetFoo_DoesntExist_Returns404()
    {
        // Act
        var response = await _appHostFixture.AppHttpClient.GetAsync("/foos/1000");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFoo_IdOutOfRange_ReturnsBadRequest()
    {
        // Act
        var response = await _appHostFixture.AppHttpClient.GetAsync("/foos/100000");
        var result = await response.Content.ReadFromJsonAsync<ValidationResultDetails>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(result);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Validly.Validations.Between.Error", error.ResourceKey);
        Assert.True(int.TryParse(error.Args[0]?.ToString(), out var from));
        Assert.True(int.TryParse(error.Args[1]?.ToString(), out var to));
        Assert.Equal(1, from);
        Assert.Equal(9999, to);
    }
}
