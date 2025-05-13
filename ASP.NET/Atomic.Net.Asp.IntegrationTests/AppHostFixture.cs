
using Aspire.Hosting;
using Atomic.Net.Asp.ServiceDefaults;
using Microsoft.Extensions.Logging;

namespace Atomic.Net.Asp.IntegrationTests;

public class AppHostFixture : IAsyncLifetime
{
    
    public DistributedApplication App { get; private set; }= default!;

    public HttpClient AppHttpClient { get; private set; } =  default!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Atomic_Net_Asp_AppHost>(
                [
                    "UseVolumes=false",
                    "SeedData=true"
                ]
            );
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        App = await appHost
            .BuildAsync();
        await App
            .StartAsync();

        AppHttpClient = App.CreateHttpClient(ServiceConstants.WEBAPI);
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            ServiceConstants.WEBAPI
        );
    }


    public async Task DisposeAsync()
    {
        AppHttpClient.Dispose();
        await App.DisposeAsync();
    }
}
