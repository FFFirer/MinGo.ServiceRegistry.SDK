using MinGo.ServiceRegistry.Abstractions;

namespace MinGo.ServiceRegistry.AspNetCore;

/// <summary>
/// Options describing how the current service registers itself with the Registry Server.
/// </summary>
public sealed class ServiceRegistryOptions
{
    /// <summary>Logical service name, e.g. <c>user-service</c>. Required.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Base address of the Registry Server, e.g. <c>https://registry:8500</c>. Required.</summary>
    public Uri? RegistryEndpoint { get; set; }

    /// <summary>Stable instance id. When <see langword="null"/> one is generated on first registration and reused.</summary>
    public string? InstanceId { get; set; }

    /// <summary>URI scheme to advertise. When <see langword="null"/> it is derived from the bound server address (else <c>http</c>).</summary>
    public string? Scheme { get; set; }

    /// <summary>Host/IP to advertise. When <see langword="null"/> it is derived from the bound server address (else the machine host name).</summary>
    public string? Host { get; set; }

    /// <summary>Port to advertise. When <see langword="null"/> it is derived from the bound server address.</summary>
    public int? Port { get; set; }

    /// <summary>Extra metadata advertised with the instance.</summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>Lease time-to-live, in seconds.</summary>
    public int LeaseTtlSeconds { get; set; } = ServiceRegistryDefaults.DefaultLeaseTtlSeconds;

    /// <summary>Heartbeat interval. When <see langword="null"/> it defaults to one third of <see cref="LeaseTtlSeconds"/>.</summary>
    public TimeSpan? HeartbeatInterval { get; set; }

    /// <summary>Per-request timeout for Registry calls.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Whether to register automatically when the host starts.</summary>
    public bool RegisterOnStart { get; set; } = true;

    /// <summary>Whether to deregister (best-effort) when the host stops.</summary>
    public bool DeregisterOnStop { get; set; } = true;
}
