using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Foos;
using Microsoft.AspNetCore.Mvc;

namespace Atomic.Net.Asp.Application.Controllers;

public static class FooController
{
    public static async Task<ApiResult<GetFooResponse>> GetFooAsync(
        HttpContext httpContext,
        [FromServices] WebContext<GetFooQuery> webContext,
        [AsParameters] GetFooQuery request
    )
    {
        return await Handlers.HandleQuery(
           httpContext, webContext, request, GetFooQueryHandler.HandleAsync
        );
    }

    public static async Task<ApiResult<Unit>> DeleteFooAsync(
        HttpContext httpContext,
        [FromServices] WebContext<DeleteFooCommand> webContext,
        [AsParameters] DeleteFooCommand cmd
    )
    {
        return await Handlers.HandleCommand(
            httpContext, webContext, cmd, DeleteFooCommandHandler.HandleAsync
        );
    }
}
