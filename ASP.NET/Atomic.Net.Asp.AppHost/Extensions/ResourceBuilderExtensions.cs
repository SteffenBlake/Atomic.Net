using Atomic.Net.Asp.ServiceDefaults;

namespace Atomic.Net.Asp.AppHost.Extensions;

public static class ResourceBuilderExtensions 
{
    public static IResourceBuilder<T> WithUrlsHost<T>(
        this IResourceBuilder<T> resource, string? host
    )
        where T: IResourceWithEndpoints
    {
        if (string.IsNullOrEmpty(host) || host == "localhost")
        {
            return resource;
        }

        resource.WithUrls(ctx => {
            foreach(var annotation in ctx.Urls)
            {
                var original = new  UriBuilder(new Uri(annotation.Url))
                {
                    Host = host 
                };
                annotation.Url = original.ToString();
                annotation.DisplayText = annotation.Endpoint?.EndpointName;
            }
        });

        return resource;
    }

    private static int _proxyPortTracker = 1;
    public static IResourceBuilder<ProjectResource> ProxyTo<T>(
        this IResourceBuilder<ProjectResource> devProxy,
        IResourceBuilder<T> target,
        string hostOverride,
        out string proxyUrlResult,
        string targetEndpointName = "http"
    )
        where T: IResourceWithEndpoints
    {
        var targetPort = _proxyPortTracker + 50000;
        proxyUrlResult = $"http://{hostOverride}:{targetPort}";

        devProxy.WithEndpoint(
            targetPort, 
            targetPort:targetPort, 
            scheme:"http", 
            name: target.Resource.Name, 
            isProxied:false, 
            isExternal:true
        );

        var selfEndpoint = devProxy.GetEndpoint(target.Resource.Name);

        devProxy.WithReference(selfEndpoint);
        devProxy.WithReference(target.GetEndpoint(targetEndpointName));

        _proxyPortTracker++;

        return devProxy;
    }
}
