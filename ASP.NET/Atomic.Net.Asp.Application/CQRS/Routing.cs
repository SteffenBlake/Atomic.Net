using Atomic.Net.Asp.Domain.Foos.Delete;
using Atomic.Net.Asp.Domain.Foos.Get;

namespace Atomic.Net.Asp.Application.CQRS; 

public static class Routing
{
    public static void AddCQRS(this IServiceCollection services)
    {
        // Foos
        _ = services.AddGetHandler("/api/foos/{fooId:int}", GetFooQueryHandler.Run);
        _ = services.AddDeleteHandler("/api/foos/{fooId:int}", DeleteFooCommandHandler.Run);

        // Bars
        // ...
    }
}
