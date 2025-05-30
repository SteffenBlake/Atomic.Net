using Atomic.Net.Asp.Application.CQRS.Mappers;

namespace Atomic.Net.Asp.Application.CQRS;

public static class WebApplicationExtensions 
{
    public static void MapRoutes(this WebApplication app)
    {
        var mappers = app.Services.GetRequiredService<IEnumerable<ICQRSMapper>>();

        foreach(var mapper in mappers)
        {
            mapper.Run(app);
        }
    }
}
