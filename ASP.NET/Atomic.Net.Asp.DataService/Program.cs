using Atomic.Net.Asp.DataService;
using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

var config = builder.Configuration.Get<DataServiceConfiguration>() 
    ?? throw new InvalidOperationException();
_ = builder.Services.AddSingleton(config);


builder.Services.AddDbContext<AppDbContext>(options =>
{
    _ = options.UseNpgsql(
        builder.Configuration.GetConnectionString(ServiceConstants.POSTGRESDB)
    );
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
