namespace Atomic.Net.Asp.DevProxy;

public record DevProxyConfig(
    string HostOverride,
    Dictionary<string, Dictionary<string, List<string>>> Services
);
