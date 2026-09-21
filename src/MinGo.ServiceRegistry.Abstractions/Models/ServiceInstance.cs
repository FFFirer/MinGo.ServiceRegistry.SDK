namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// A discoverable service instance returned by the Registry Server.
/// </summary>
public sealed class ServiceInstance
{
    /// <summary>Logical service name.</summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>Instance identifier.</summary>
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>URI scheme of the endpoint.</summary>
    public string Scheme { get; init; } = ServiceRegistryDefaults.DefaultScheme;

    /// <summary>Host or IP address of the endpoint.</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>Port of the endpoint.</summary>
    public int Port { get; init; }

    /// <summary>Instance metadata.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>Current health state. Discovery only returns <see cref="ServiceHealthStatus.Healthy"/> instances.</summary>
    public ServiceHealthStatus Health { get; init; } = ServiceHealthStatus.Healthy;
}
