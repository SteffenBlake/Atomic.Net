using Atomic.Net.Asp.Domain;
using Microsoft.EntityFrameworkCore;
using Atomic.Net.Asp.ServiceDefaults;
using Atomic.Net.Asp.Application.CQRS;
using Atomic.Net.Asp.Domain.Extensions;
using dotVariant;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Atomic.Net.Asp.Domain.Results;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    _ = options.UseNpgsql(
        builder.Configuration.GetConnectionString(ServiceConstants.POSTGRESDB)
    );
});

builder.Services.AddCommonDomainLayer();

builder.Services.AddCQRS();

builder.Services.Configure<JsonOptions>(options => {
    options.SerializerOptions.Converters.Add(new DomainResultConverterFactory());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapRoutes();

app.Run();
