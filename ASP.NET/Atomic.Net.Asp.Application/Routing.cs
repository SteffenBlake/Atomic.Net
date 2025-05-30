using Atomic.Net.Asp.Application.Controllers;

namespace Atomic.Net.Asp.Application;

public static class Routing
{
    public static void AddRoutes(this WebApplication app)
    {
        _ = app.MapGet("/api", () => "Hello world!");

        // Foos
        _ = app.MapGet("/api/foos/{fooId:int}", FooController.GetFooAsync);
        _ = app.MapDelete("/api/foos/{fooId:int}", FooController.DeleteFooAsync);

        // Bars
        // ...
    }
}
