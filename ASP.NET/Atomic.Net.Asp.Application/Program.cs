using Atomic.Net.Asp.Application;
using Atomic.Net.Asp.Domain;
using Microsoft.EntityFrameworkCore;
using Atomic.Net.Asp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    _ = options.UseNpgsql(
        builder.Configuration.GetConnectionString(ServiceConstants.POSTGRESDB)
    );
});

builder.Services.AddScoped(typeof(WebContext<>));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
}

app.UseHttpsRedirection();

app.AddRoutes();

app.Run();
