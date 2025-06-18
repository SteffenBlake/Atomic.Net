using Atomic.Net.Asp.Application.CQRS.Handlers;

namespace Atomic.Net.Asp.Application.CQRS.Mappers;

public class CQRSMapperPost<TDomainContext, TRequest, TResult>(
    string pattern
) : ICQRSMapper
    where TDomainContext : class
    where TRequest : class
{
    public void Run(WebApplication app)
    {
        app.MapPost(pattern, CommandHandler.HandleAsync<TDomainContext, TRequest, TResult>);
    }
}

