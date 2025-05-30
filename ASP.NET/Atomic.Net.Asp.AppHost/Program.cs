using Atomic.Net.Asp.AppHost.Extensions;
using Atomic.Net.Asp.ServiceDefaults;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
var useVolumes = builder.Configuration.GetValue<bool>("UseVolumes");
var seedData = builder.Configuration.GetValue<string>("SeedData");

var postgresPort = builder.Configuration.GetValue<int>("Postgres:Port");
var postgresPassword = builder.AddParameter("PostgresPassword", secret: true);

var hostOverride = builder.Configuration["HostOverride"]?.Trim();

var postgres = builder.AddPostgres(
    ServiceConstants.POSTGRES, port:postgresPort, password: postgresPassword
);

var postgresDb = postgres.AddDatabase(ServiceConstants.POSTGRESDB);

var dataService = builder.AddProject<Projects.Atomic_Net_Asp_DataService>(
        ServiceConstants.DATASERVICE
    )
    .WithReference(postgresDb)
    .WithEnvironment("SeedData", seedData)
    .WaitFor(postgresDb);

var application = builder.AddProject<Projects.Atomic_Net_Asp_Application>(
        ServiceConstants.WEBAPI
    )
    .WithReference(postgresDb)
    .WaitFor(postgresDb)
    .WaitForCompletion(dataService);

var spwa = builder.AddNpmApp(ServiceConstants.SPWA, "../Atomic.Net.Asp.SPWA")
    .WithReference(application)
    .WaitFor(application)
    .WithHttpEndpoint(env: "PORT")
    .PublishAsDockerFile();

if (useVolumes)
{
    _ = postgres.WithDataVolume(ServiceConstants.POSTGRES, isReadOnly: false);
}

if (!string.IsNullOrEmpty(hostOverride))
{
    var devProxy = builder.AddProject<Projects.Atomic_Net_Asp_DevProxy>(
        ServiceConstants.DEVPROXY
    )
        .WithEnvironment("HostOverride", hostOverride)
        .ProxyTo(application, hostOverride, out var applicationProxiedUrl)
        .ProxyTo(spwa, hostOverride, out var spwaProxiedUrl)
        .WithUrlsHost(hostOverride)
        .WithUrlForEndpoint("http", url => url.DisplayOrder = null);

    postgres.WithPgAdmin(pgAdmin => devProxy.ProxyTo(pgAdmin, hostOverride, out _));

    application.WithEnvironment("ProxiedSpwaUrl", spwaProxiedUrl);
    spwa.WithEnvironment("ProxiedUrl", applicationProxiedUrl);
}

builder.Build().Run();
