namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// Lease parameters requested by a client at registration time.
/// </summary>
public sealed class LeaseOptions
{
    /// <summary>
    /// Time-to-live of the lease, in seconds. The server removes the instance once
    /// no renewal is observed within this window.
    /// </summary>
    public int TtlSeconds { get; init; } = ServiceRegistryDefaults.DefaultLeaseTtlSeconds;
}
