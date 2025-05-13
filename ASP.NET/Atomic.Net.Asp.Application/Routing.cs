using Atomic.Net.Asp.Application.Controllers;

namespace Atomic.Net.Asp.Application;

public static class Routing
{
    public static void AddRoutes(this WebApplication app)
    {
        _ = app.MapGet("/", () => "Hello world!");

        // Foos
        _ = app.MapGet("/foos/{fooId:int}", FooController.GetFooAsync);
        _ = app.MapDelete("/foos/{fooId:int}", FooController.DeleteFooAsync);

        // Bars
        // ...
    }
}
