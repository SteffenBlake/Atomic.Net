
using Aspire.Hosting;
using Atomic.Net.Asp.ServiceDefaults;
using Microsoft.Extensions.Logging;

namespace Atomic.Net.Asp.IntegrationTests;

public class AppHostFixture : IAsyncLifetime
{
    
    public DistributedApplication App { get; private set; }= default!;

    public string SpwaUrl { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Atomic_Net_Asp_AppHost>(
                [
                    "UseVolumes=false",
                    "SeedData=true",
                    "Postgres:Port=5444",
                    "Environment=Test",
                    "HostOverride=localhost",
                ]
            );
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        App = await appHost.BuildAsync();
        await App.StartAsync();

        SpwaUrl = App.GetEndpoint(
            ServiceConstants.DEVPROXY, ServiceConstants.SPWA
        ).AbsoluteUri;
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
    }
}
