using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Foos.Delete;
using Atomic.Net.Asp.Domain.Foos.Get;
using Microsoft.AspNetCore.Mvc;

namespace Atomic.Net.Asp.Application;

public static class Routing
{
    public static void AddRoutes(this WebApplication app)
    {
        // Foos
        app.MapCQRSGet<GetFooQuery, GetFooResponse>("/api/foos/{fooId:int}");
        app.MapCQRSDelete<DeleteFooCommand, Unit>("/api/foos/{fooId:int}");

        // Bars
        // ...
    }


    private static void MapCQRSPut<TRequest, TResult>(this WebApplication app, string pattern)
        where TRequest: class
    {
        app.MapPut(pattern, HandleCommandAsync<TRequest, TResult>);
    }

    private static void MapCQRSPost<TRequest, TResult>(this WebApplication app, string pattern)
        where TRequest: class
    {
        app.MapPost(pattern, HandleCommandAsync<TRequest, TResult>);
    }

    private static void MapCQRSGet<TRequest, TResult>(this WebApplication app, string pattern)
        where TRequest: class
    {
        app.MapGet(pattern, HandleQueryAsync<TRequest, TResult>);
    }

    private static void MapCQRSDelete<TRequest, TResult>(
        this WebApplication app, string pattern
    )
        where TRequest: class
    {
        app.MapDelete(pattern, HandleCommandAsync<TRequest, TResult>);
    }

    private static void MapCQRSPatch<TRequest, TResult>(
        this WebApplication app, string pattern
    )
        where TRequest: class
    {
        app.MapPatch(pattern, HandleCommandAsync<TRequest, TResult>);
    }

    private static async Task<IDomainResult<TResult>> HandleQueryAsync<TRequest, TResult>(
        HttpContext httpContext,
        [FromServices]RequestRunner<TRequest, TResult> runner, 
        [AsParameters]TRequest request
    )
        where TRequest: class
    {
        return await runner.HandleQueryAsync(httpContext, request);
    }

    private static async Task<IDomainResult<TResult>> HandleCommandAsync<TRequest, TResult>(
        HttpContext httpContext, 
        [FromServices]RequestRunner<TRequest, TResult> runner,
        [AsParameters]TRequest request
    )
        where TRequest: class
    {
        return await runner.HandleCommandAsync(httpContext, request);
    }
}
