using Atomic.Net.Asp.Application;
using Atomic.Net.Asp.Domain;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("ATOMIC_ASPNET_");

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    _ = options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped(typeof(WebContext<>));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
}

app.UseHttpsRedirection();

app.AddRoutes();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.RunAsync(db);
}

app.Run();
