using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Foos.Delete;
using Atomic.Net.Asp.Domain.Foos.Get;

namespace Atomic.Net.Asp.Application;

public static class CQRS
{
    public static void AddCQRS(this WebApplicationBuilder builder)
    {

        builder.Services.AddScoped(typeof(RequestRunner<,>));

        builder.Services.AddSingleton<
            IRequestHandler<GetFooQuery, GetFooResponse>,
            GetFooQueryHandler
        >();
        builder.Services.AddSingleton<
            IRequestHandler<DeleteFooCommand, Unit>,
            DeleteFooCommandHandler
        >();
    }
}
