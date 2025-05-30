using System.Text.Json;
using Atomic.Net.Asp.DevProxy;
using Atomic.Net.Asp.ServiceDefaults;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddCors(options => 
    options.AddDefaultPolicy(policy =>
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()
    )
);

var devProxyConfig = builder.Configuration.Get<DevProxyConfig>() ??
    throw new InvalidOperationException(
        "Invalid configuration schema"
    );

Console.WriteLine(
    "Using Config: " + JsonSerializer.Serialize(
        devProxyConfig,
        new JsonSerializerOptions()
        {
            WriteIndented = true
        }
    ));

List<RouteConfig> routeConfigs = [];
List<ClusterConfig> clusterConfigs = [];

// Map all localhost internal hosts to <host override>:<port+PortIncrement>
// IE assuming HostOverride = "DevMachine" and PortIncrement=1000 then:
// http://DevMachine:11001 -> http://localhost:10001

var targetKeys = devProxyConfig.Services.Keys.ToArray();
var devProxyEntries = devProxyConfig.Services[ServiceConstants.DEVPROXY];
List<string> listenUrls = [];

for (var n = 0; n < targetKeys.Length; n++)
{
    var targetKey = targetKeys[n]!;
    // Dont try and proxy ourself
    if (targetKey == ServiceConstants.DEVPROXY)
    {
        continue;
    }
    var service = devProxyConfig.Services[targetKey];
    var target = service.First().Value[0];
    var from = new UriBuilder(new Uri(devProxyEntries[targetKey][0]))
    {
        Host = devProxyConfig.HostOverride
    };

    var routeId = $"route{n}";
    var clusterId = $"cluster{n}";
    var destination = $"cluster{n}/destination1";

    Console.WriteLine(
        $"Proxying: {from.Uri.Authority} -> {target} ({targetKey})"
    );

    routeConfigs.Add(new()
    {
        RouteId = routeId,
        ClusterId = clusterId,
        Match = new()
        {
            Hosts = [from.Uri.Authority],
            Path = "{**catch-all}"
        },
        // Transforms = [
        //     new Dictionary<string, string>
        //     {
        //         { "RequestHeader", "X-Forwarded-Proto" },
        //         { "Set", "https" }
        //     },
        //     new Dictionary<string, string>
        //     {
        //         { "RequestHeader", "X-Forwarded-Host" },
        //         { "Set", from.Uri.Authority }
        //     }
        // ]
    });
    clusterConfigs.Add(new()
    {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, DestinationConfig>(){
            {
                destination,
                new()
                {
                    Address = target
                }
            }
        }
    });
    listenUrls.Add(from.ToString());
}

builder.Services.AddReverseProxy()
    .LoadFromMemory(routeConfigs, clusterConfigs);

builder.WebHost.UseUrls([.. listenUrls]);

var app = builder.Build();

app.MapReverseProxy();

app.UseCors();

app.Run();
