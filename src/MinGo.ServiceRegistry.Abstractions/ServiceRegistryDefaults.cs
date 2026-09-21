namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// Well-known defaults shared by the SDK and the Registry Server.
/// </summary>
public static class ServiceRegistryDefaults
{
    /// <summary>Default lease time-to-live, in seconds.</summary>
    public const int DefaultLeaseTtlSeconds = 15;

    /// <summary>Default interval at which the server reaps expired leases, in seconds.</summary>
    public const int DefaultReaperIntervalSeconds = 5;

    /// <summary>Default interval at which discovery results are refreshed, in seconds.</summary>
    public const int DefaultDiscoveryRefreshSeconds = 15;

    /// <summary>Default URI scheme used when none is supplied.</summary>
    public const string DefaultScheme = "http";
}
