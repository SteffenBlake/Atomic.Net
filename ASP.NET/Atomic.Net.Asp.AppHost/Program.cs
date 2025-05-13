using Atomic.Net.Asp.ServiceDefaults;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
var useVolumes = builder.Configuration.GetValue<bool>("UseVolumes");
var seedData = builder.Configuration.GetValue<string>("SeedData");

var postgresPort = builder.Configuration.GetValue<int>("Postgres:Port");

var postgres = builder.AddPostgres(ServiceConstants.POSTGRES, port:postgresPort);
if (useVolumes)
{
    _ = postgres.WithDataVolume(ServiceConstants.POSTGRES, isReadOnly: false);
}

var postgresDb = postgres.AddDatabase(ServiceConstants.POSTGRESDB);

var dataService = builder.AddProject<Projects.Atomic_Net_Asp_DataService>(
        ServiceConstants.DATASERVICE
    )
    .WithReference(postgresDb)
    .WithEnvironment("SeedData", seedData)
    .WaitFor(postgresDb);

var application = builder.AddProject<Projects.Atomic_Net_Asp_Application>(
        ServiceConstants.WEBAPI, launchProfileName: "http"
    )
    .WithExternalHttpEndpoints()
    .WithReference(postgresDb)
    .WaitFor(postgresDb)
    .WaitForCompletion(dataService);

builder.Build().Run();
