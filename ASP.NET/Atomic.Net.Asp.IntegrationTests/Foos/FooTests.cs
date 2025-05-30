using Atomic.Net.Asp.ServiceDefaults;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Atomic.Net.Asp.IntegrationTests.Foos;

[Collection("Global Test Collection")]
public class FooTests(AppHostFixture appHostFixture) : PageTest
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
            .GetByTestId("foo-result-id")
            .WaitForAsync();

        var id = await Page
            .GetByTestId("foo-result-id")
            .TextContentAsync();
        var fullName = await Page
            .GetByTestId("foo-result-fullname")
            .TextContentAsync();
        var sensitive = await Page
            .GetByTestId("foo-result-sensitive")
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
            .GetByTestId("server-errors")
            .WaitForAsync();

        var errorMsg = await Page
            .GetByTestId("server-errors")
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
            .GetByTestId("validation-errors")
            .WaitForAsync();

        var titleSpan = Page
            .GetByTestId("validation-errors")
            .Locator("span")
            .First;
        await titleSpan.WaitForAsync();
        var title = await titleSpan.TextContentAsync();

        var errorDetailsSpan = Page.GetByTestId("validation-errors-details-0");
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
            .GetByTestId("foo-id-input")
            .FillAsync(fooId.ToString());
        await Page
            .GetByTestId("submit-btn")
            .ClickAsync();
    }
}
