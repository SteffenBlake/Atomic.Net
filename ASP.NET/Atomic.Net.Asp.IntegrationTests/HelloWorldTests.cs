namespace Atomic.Net.Asp.IntegrationTests;

[Collection("Global Test Collection")]
public class HelloWorldTests(AppHostFixture appHostFixture)
{
    private readonly AppHostFixture _appHostFixture = appHostFixture;

    [Fact]
    public async Task GetWebResourceRootReturnsHelloWorld()
    {
        // Act
        var response = await _appHostFixture.AppHttpClient.GetAsync("/");
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello world!", result);
    }
}
