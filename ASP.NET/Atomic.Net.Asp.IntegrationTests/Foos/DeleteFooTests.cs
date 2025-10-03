using Atomic.Net.Asp.ServiceDefaults;
using Microsoft.Playwright.Xunit;

namespace Atomic.Net.Asp.IntegrationTests.Foos;

[Collection("AppHostCollection")]
public class DeleteFooTests(AppHostFixture appHostFixture) : PageTest
{
    private readonly AppHostFixture _appHostFixture = appHostFixture;

    [Fact]
    public async Task DeleteFoo_Exists_ShowsValidData()
    {
        await _appHostFixture.App.ResourceNotifications.WaitForResourceHealthyAsync(
            ServiceConstants.SPWA
        );

        await SubmitFormAsync(1);
       
        var successSpan = Page
            .GetByTestId("delete-foo-success");

        await successSpan.WaitForAsync();
        var successMsg = await successSpan.TextContentAsync();

        Assert.Equal("Success!", successMsg);

        await _appHostFixture.RestoreDb();
    }

    [Fact]
    public async Task DeleteFoo_Twice_ShowsErrorMsg()
    {
        await _appHostFixture.App.ResourceNotifications.WaitForResourceHealthyAsync(
            ServiceConstants.SPWA
        );

        await SubmitFormAsync(1);
       
        var successSpan = Page
            .GetByTestId("delete-foo-success");

        await successSpan.WaitForAsync();
        var successMsg = await successSpan.TextContentAsync();

        Assert.Equal("Success!", successMsg);

        await SubmitFormAsync(1);

        await Page
            .GetByTestId("delete-foo-server-errors")
            .WaitForAsync();

        var errorMsg = await Page
            .GetByTestId("delete-foo-server-errors")
            .TextContentAsync();

        Assert.Equal(
            "No FooEntity found with id '1'", 
            errorMsg
        );

        await _appHostFixture.RestoreDb();
    }

    [Fact]
    public async Task GetFoo_DoesntExist_ShowsNotFoundMessage()
    {
        await _appHostFixture.App.ResourceNotifications.WaitForResourceHealthyAsync(
            ServiceConstants.SPWA
        );

        await SubmitFormAsync(500);

        await Page
            .GetByTestId("delete-foo-server-errors")
            .WaitForAsync();

        var errorMsg = await Page
            .GetByTestId("delete-foo-server-errors")
            .TextContentAsync();

        Assert.Equal(
            "No FooEntity found with id '500'", 
            errorMsg
        );

        await _appHostFixture.RestoreDb();
    }

    [Fact]
    public async Task DeleteFoo_IdOutOfRange_ShowsProblemDetails()
    {
        await _appHostFixture.App.ResourceNotifications.WaitForResourceHealthyAsync(
            ServiceConstants.SPWA
        );

        await SubmitFormAsync(50000);

        await Page
            .GetByTestId("delete-foo-validation-errors")
            .WaitForAsync();

        var titleSpan = Page
            .GetByTestId("delete-foo-validation-errors")
            .Locator("span")
            .First;
        await titleSpan.WaitForAsync();
        var title = await titleSpan.TextContentAsync();

        var errorDetailsSpan = Page.GetByTestId("delete-foo-validation-errors-details-0");
        await errorDetailsSpan.WaitForAsync();

        var errorDetails = await errorDetailsSpan.TextContentAsync();

        Assert.False(string.IsNullOrEmpty(title));
        Assert.Equal("FooId Must be between 1 and 9999.", errorDetails);

        await _appHostFixture.RestoreDb();
    }

    private async Task SubmitFormAsync(int fooId)
    {
        var url = _appHostFixture.SpwaUrl;
        await Page.GotoAsync(url);

        await Page
            .GetByTestId("delete-foo-id-input")
            .FillAsync(fooId.ToString());
        await Page
            .GetByTestId("delete-foo-submit-btn")
            .ClickAsync();
    }
}
