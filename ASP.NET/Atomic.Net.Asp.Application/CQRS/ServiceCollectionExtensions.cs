using Atomic.Net.Asp.Application.CQRS.Mappers;
using Atomic.Net.Asp.Domain;
namespace Atomic.Net.Asp.Application.CQRS;

public static class ServiceCollectionExtensions 
{
    public static IServiceCollection AddPutHandler<TResult, TServices, TRequest>(
        this IServiceCollection services,
        string pattern,
        RequestHandler<TResult, TServices, TRequest> handler
    )
        where TServices : class
        where TRequest : class
    {
        return services
            .AddSingleton(handler)
            .AddScoped<TServices>()
            .AddSingleton<ICQRSMapper>(
                new CQRSMapperPut<TResult, TServices, TRequest>(pattern)
            );
    }

    public static IServiceCollection AddGetHandler<TResult, TServices, TRequest>(
        this IServiceCollection services,
        string pattern,
        RequestHandler<TResult, TServices, TRequest> handler
    )
        where TServices : class
        where TRequest : class
    {
        return services
            .AddSingleton(handler)
            .AddScoped<TServices>()
            .AddSingleton<ICQRSMapper>(
                new CQRSMapperGet<TResult, TServices, TRequest>(pattern)
            );
    }

    public static IServiceCollection AddPostHandler<TResult, TServices, TRequest>(
        this IServiceCollection services,
        string pattern,
        RequestHandler<TResult, TServices, TRequest> handler
    )
        where TServices : class
        where TRequest : class
    {
        return services
            .AddSingleton(handler)
            .AddScoped<TServices>()
            .AddSingleton<ICQRSMapper>(
                new CQRSMapperPost<TResult, TServices, TRequest>(pattern)
            );
    }

    public static IServiceCollection AddPatchHandler<TResult, TServices, TRequest>(
        this IServiceCollection services,
        string pattern,
        RequestHandler<TResult, TServices, TRequest> handler
    )
        where TServices : class
        where TRequest : class
    {
        return services
            .AddSingleton(handler)
            .AddScoped<TServices>()
            .AddSingleton<ICQRSMapper>(
                new CQRSMapperPatch<TResult, TServices, TRequest>(pattern)
            );
    }

    public static IServiceCollection AddDeleteHandler<TResult, TServices, TRequest>(
        this IServiceCollection services,
        string pattern,
        RequestHandler<TResult, TServices, TRequest> handler
    )
        where TServices : class
        where TRequest : class
    {
        return services
            .AddSingleton(handler)
            .AddScoped<TServices>()
            .AddSingleton<ICQRSMapper>(
                new CQRSMapperDelete<TResult, TServices, TRequest>(pattern)
            );
    }
}
