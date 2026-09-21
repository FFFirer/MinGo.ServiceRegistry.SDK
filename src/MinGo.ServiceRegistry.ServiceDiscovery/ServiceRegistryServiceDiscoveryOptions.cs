using MinGo.ServiceRegistry.Abstractions;

namespace MinGo.ServiceRegistry.ServiceDiscovery;

/// <summary>
/// Options for the Registry-backed Microsoft Service Discovery integration.
/// </summary>
public sealed class ServiceRegistryServiceDiscoveryOptions
{
    /// <summary>
    /// How often resolved endpoints are considered stale and re-fetched from the Registry Server.
    /// Implemented as a polling change token; there is no server push in the MVP.
    /// </summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(ServiceRegistryDefaults.DefaultDiscoveryRefreshSeconds);
}
