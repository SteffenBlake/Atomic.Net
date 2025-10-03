
using Aspire.Hosting;
using Atomic.Net.Asp.ServiceDefaults;
using Microsoft.Extensions.Logging;

namespace Atomic.Net.Asp.IntegrationTests;

public class AppHostFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; }= default!;

    public string SpwaUrl { get; private set; } = default!;

    public async Task RestoreDb()
    {
        await ExecuteContainerAsync(
            ServiceConstants.POSTGRESRESTORE, 
            new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token
        );
    }

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

        await App.ResourceNotifications.WaitForResourceAsync(
            ServiceConstants.DATASERVICE, KnownResourceStates.Finished, 
            new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token
        );

        await ExecuteContainerAsync(
            ServiceConstants.POSTGRESBACKUP, 
            new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token
        );

        SpwaUrl = App.GetEndpoint(
            ServiceConstants.DEVPROXY, ServiceConstants.SPWA
        ).AbsoluteUri;
    }

    private async Task ExecuteContainerAsync(string name, CancellationToken cancellationToken)
    {
        var cmdService = App.Services.GetRequiredService<ResourceCommandService>();

        var result = await cmdService.ExecuteCommandAsync(
            name, "resource-start", cancellationToken
        );
        if (!result.Success)
        {
            throw new Exception(result.ErrorMessage);
        }

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        await App.ResourceNotifications.WaitForResourceAsync(
            name, KnownResourceStates.Exited, cancellationToken
        );
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
    }
}
