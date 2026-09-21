namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// Client abstraction over the Service Registry HTTP protocol.
/// </summary>
/// <remarks>
/// The client intentionally does <em>not</em> implement retry, circuit breaking, load
/// balancing or endpoint selection. Those concerns belong to
/// <c>Microsoft.Extensions.Http.Resilience</c> and <c>Microsoft.Extensions.ServiceDiscovery</c>.
/// </remarks>
public interface IServiceRegistryClient
{
    /// <summary>Registers a service instance and returns the server-issued lease.</summary>
    Task<RegistrationResult> RegisterAsync(ServiceRegistration registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renews (heartbeats) a lease. Throws <see cref="RegistryLeaseLostException"/> when the
    /// server no longer knows the lease, signalling that the caller must re-register.
    /// </summary>
    Task<RenewResult> RenewLeaseAsync(string leaseId, CancellationToken cancellationToken = default);

    /// <summary>Deregisters an instance by its lease. Best-effort; a lost lease is not an error.</summary>
    Task DeregisterAsync(string leaseId, CancellationToken cancellationToken = default);

    /// <summary>Discovers the healthy instances of a service.</summary>
    Task<IReadOnlyList<ServiceInstance>> DiscoverAsync(string serviceName, CancellationToken cancellationToken = default);
}
