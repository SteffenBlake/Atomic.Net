using Atomic.Net.Asp.Application.CQRS.Handlers;

namespace Atomic.Net.Asp.Application.CQRS.Mappers;

public class CQRSMapperDelete<TResult, TServices, TRequest>(
    string pattern
) : ICQRSMapper
    where TServices : class
    where TRequest : class
{
    public void Run(WebApplication app)
    {
        app.MapDelete(pattern, CommandHandler.HandleAsync<TResult, TServices, TRequest>);
    }
}

