using Atomic.Net.Asp.Application.CQRS.Handlers;

namespace Atomic.Net.Asp.Application.CQRS.Mappers;

public class CQRSMapperGet<TResult, TServices, TRequest>(
    string pattern
) : ICQRSMapper
    where TServices : class
    where TRequest : class
{
    public void Run(WebApplication app)
    {
        app.MapGet(pattern, QueryHandler.HandleAsync<TResult, TServices, TRequest>);
    }
}

