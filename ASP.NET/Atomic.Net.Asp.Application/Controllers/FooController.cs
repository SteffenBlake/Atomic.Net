using Atomic.Net.Asp.Domain.Foos.Get;
using Atomic.Net.Asp.Domain.Foos.Delete;
using Microsoft.AspNetCore.Mvc;
using Atomic.Net.Asp.Domain;

namespace Atomic.Net.Asp.Application.Controllers;

public static class FooController
{
    public static async Task<IDomainResult<GetFooResponse>> GetFooAsync(
        HttpContext httpContext,
        [FromServices] WebContext<GetFooQuery> webContext,
        [AsParameters] GetFooQuery request
    )
    {
        return await Handlers.HandleQueryAsync(
           httpContext, webContext, request, GetFooQueryHandler.HandleAsync
        );
    }

    public static async Task<IDomainResult<Unit>> DeleteFooAsync(
        HttpContext httpContext,
        [FromServices] WebContext<DeleteFooCommand> webContext,
        [AsParameters] DeleteFooCommand cmd
    )
    {
        return await Handlers.HandleCommandAsync(
            httpContext, webContext, cmd, DeleteFooCommandHandler.HandleAsync
        );
    }
}
