using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.ServiceDiscovery;
using MinGo.ServiceRegistry.Abstractions;

namespace MinGo.ServiceRegistry.ServiceDiscovery.Tests;

/// <summary>Test double for <see cref="IServiceEndpointBuilder"/>.</summary>
internal sealed class TestEndpointBuilder : IServiceEndpointBuilder
{
    public IList<ServiceEndpoint> Endpoints { get; } = new List<ServiceEndpoint>();

    public IFeatureCollection Features { get; } = new FeatureCollection();

    /// <summary>Change tokens handed to <see cref="AddChangeToken"/>.</summary>
    public List<IChangeToken> ChangeTokens { get; } = [];

    public void AddChangeToken(IChangeToken changeToken) => ChangeTokens.Add(changeToken);
}

/// <summary>
/// <see cref="IServiceRegistryClient"/> stub that returns a fixed instance list from discovery;
/// the lifecycle members are not exercised here.
/// </summary>
internal sealed class FakeDiscoveryClient : IServiceRegistryClient
{
    private readonly IReadOnlyList<ServiceInstance> _instances;

    public FakeDiscoveryClient(params ServiceInstance[] instances) => _instances = instances;

    /// <summary>Service names passed to <see cref="DiscoverAsync"/>.</summary>
    public List<string> DiscoveredServices { get; } = [];

    public Task<IReadOnlyList<ServiceInstance>> DiscoverAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        DiscoveredServices.Add(serviceName);
        return Task.FromResult(_instances);
    }

    public Task<RegistrationResult> RegisterAsync(ServiceRegistration registration, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<RenewResult> RenewLeaseAsync(string leaseId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task DeregisterAsync(string leaseId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
