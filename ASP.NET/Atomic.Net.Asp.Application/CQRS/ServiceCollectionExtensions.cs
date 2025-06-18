using Atomic.Net.Asp.Application.CQRS.Mappers;
using Atomic.Net.Asp.Domain;
namespace Atomic.Net.Asp.Application.CQRS;

public static class ServiceCollectionExtensions 
{
    public static IServiceCollection AddPutHandler<TDomainContext, TRequest, TResult>(
        this IServiceCollection services,
        string pattern,
        QueryHandler<TDomainContext, TRequest, TResult> handler
    )
        where TDomainContext : class
        where TRequest : class
    {
        return services
            .AddSingleton(handler)
            .AddScoped<TDomainContext>()
            .AddSingleton<ICQRSMapper>(
                new CQRSMapperPut<TDomainContext, TRequest, TResult>(pattern)
            );
    }

    public static IServiceCollection AddGetHandler<TDomainContext, TRequest, TResult>(
        this IServiceCollection services,
        string pattern,
        QueryHandler<TDomainContext, TRequest, TResult> handler
    )
        where TDomainContext : class
        where TRequest : class
    {
        return services
            .AddSingleton(handler)
            .AddScoped<TDomainContext>()
            .AddSingleton<ICQRSMapper>(
                new CQRSMapperGet<TDomainContext, TRequest, TResult>(pattern)
            );
    }

    public static IServiceCollection AddPostHandler<TDomainContext, TRequest, TResult>(
        this IServiceCollection services,
        string pattern,
        QueryHandler<TDomainContext, TRequest, TResult> handler
    )
        where TDomainContext : class
        where TRequest : class
    {
        return services
            .AddSingleton(handler)
            .AddScoped<TDomainContext>()
            .AddSingleton<ICQRSMapper>(
                new CQRSMapperPost<TDomainContext, TRequest, TResult>(pattern)
            );
    }

    public static IServiceCollection AddPatchHandler<TDomainContext, TRequest, TResult>(
        this IServiceCollection services,
        string pattern,
        QueryHandler<TDomainContext, TRequest, TResult> handler
    )
        where TDomainContext : class
        where TRequest : class
    {
        return services
            .AddSingleton(handler)
            .AddScoped<TDomainContext>()
            .AddSingleton<ICQRSMapper>(
                new CQRSMapperPatch<TDomainContext, TRequest, TResult>(pattern)
            );
    }

    public static IServiceCollection AddDeleteHandler<TDomainContext, TRequest, TResult>(
        this IServiceCollection services,
        string pattern,
        CommandHandler<TDomainContext, TRequest, TResult> handler
    )
        where TDomainContext : class
        where TRequest : class
    {
        return services
            .AddSingleton(handler)
            .AddScoped<TDomainContext>()
            .AddSingleton<ICQRSMapper>(
                new CQRSMapperDelete<TDomainContext, TRequest, TResult>(pattern)
            );
    }
}
