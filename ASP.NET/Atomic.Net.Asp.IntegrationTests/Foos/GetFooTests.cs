using Atomic.Net.Asp.ServiceDefaults;
using Microsoft.Playwright.Xunit;

namespace Atomic.Net.Asp.IntegrationTests.Foos;

[Collection("AppHostCollection")]
public class GetFooTests(AppHostFixture appHostFixture) : PageTest
{
    private readonly AppHostFixture _appHostFixture = appHostFixture;

    [Fact]
    public async Task GetFoo_Exists_ShowsValidData()
    {
        await _appHostFixture.App.ResourceNotifications.WaitForResourceHealthyAsync(
            ServiceConstants.SPWA
        );

        await SubmitFormAsync(1);

        await Page
            .GetByTestId("get-foo-result-id")
            .WaitForAsync();

        var id = await Page
            .GetByTestId("get-foo-result-id")
            .TextContentAsync();
        var fullName = await Page
            .GetByTestId("get-foo-result-fullname")
            .TextContentAsync();
        var sensitive = await Page
            .GetByTestId("get-foo-result-sensitive")
            .TextContentAsync();

        Assert.Equal("1", id);
        Assert.False(string.IsNullOrEmpty(fullName));
        Assert.False(string.IsNullOrEmpty(sensitive));
    }

    [Fact]
    public async Task GetFoo_DoesntExist_ShowsNotFoundMessage()
    {
        await _appHostFixture.App.ResourceNotifications.WaitForResourceHealthyAsync(
            ServiceConstants.SPWA
        );

        await SubmitFormAsync(500);

        await Page
            .GetByTestId("get-foo-server-errors")
            .WaitForAsync();

        var errorMsg = await Page
            .GetByTestId("get-foo-server-errors")
            .TextContentAsync();

        Assert.Equal(
            "No FooEntity found with id '500'", 
            errorMsg
        );
    }

    [Fact]
    public async Task GetFoo_IdOutOfRange_ShowsProblemDetails()
    {
        await _appHostFixture.App.ResourceNotifications.WaitForResourceHealthyAsync(
            ServiceConstants.SPWA
        );

        await SubmitFormAsync(50000);

        await Page
            .GetByTestId("get-foo-validation-errors")
            .WaitForAsync();

        var titleSpan = Page
            .GetByTestId("get-foo-validation-errors")
            .Locator("span")
            .First;
        await titleSpan.WaitForAsync();
        var title = await titleSpan.TextContentAsync();

        var errorDetailsSpan = Page.GetByTestId("get-foo-validation-errors-details-0");
        await errorDetailsSpan.WaitForAsync();

        var errorDetails = await errorDetailsSpan.TextContentAsync();

        Assert.False(string.IsNullOrEmpty(title));
        Assert.Equal("FooId Must be between 1 and 9999.", errorDetails);
    }

    private async Task SubmitFormAsync(int fooId)
    {
        var url = _appHostFixture.SpwaUrl;
        await Page.GotoAsync(url);

        await Page
            .GetByTestId("get-foo-id-input")
            .FillAsync(fooId.ToString());
        await Page
            .GetByTestId("get-foo-submit-btn")
            .ClickAsync();
    }
}
