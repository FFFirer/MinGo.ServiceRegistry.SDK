namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// The registration payload sent by a client to register a service instance.
/// </summary>
public sealed class ServiceRegistration
{
    /// <summary>Logical service name, e.g. <c>user-service</c>.</summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>
    /// Optional stable instance identifier. When <see langword="null"/> the server generates one.
    /// </summary>
    public string? InstanceId { get; init; }

    /// <summary>URI scheme of the instance endpoint, e.g. <c>http</c> or <c>https</c>.</summary>
    public string Scheme { get; init; } = ServiceRegistryDefaults.DefaultScheme;

    /// <summary>Host or IP address of the instance endpoint.</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>Port of the instance endpoint.</summary>
    public int Port { get; init; }

    /// <summary>Arbitrary key/value metadata (version, zone, tags, ...).</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>Requested lease parameters. When <see langword="null"/> server defaults apply.</summary>
    public LeaseOptions? Lease { get; init; }
}
