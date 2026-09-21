namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// Wire response for <c>GET /api/registry/services/{serviceName}/instances</c>.
/// </summary>
public sealed class ServiceInstancesResponse
{
    /// <summary>The service name the instances belong to.</summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>The healthy, non-expired instances of the service.</summary>
    public IReadOnlyList<ServiceInstance> Instances { get; init; } = [];
}
