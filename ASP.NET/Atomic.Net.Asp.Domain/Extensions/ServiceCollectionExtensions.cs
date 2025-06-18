using Atomic.Net.Asp.Domain.ThirdParty;
using Microsoft.Extensions.DependencyInjection;

namespace Atomic.Net.Asp.Domain.Extensions;

public static class ServiceCollectionExtensions 
{
    public static IServiceCollection AddCommonDomainLayer(this IServiceCollection services)
    {
        return services
            .AddScoped<ScopedDbContext>()
            .AddTransient<DomainScope>(_ => [])
            .AddScoped<ThirdPartyHttpClient>();
    }
}
