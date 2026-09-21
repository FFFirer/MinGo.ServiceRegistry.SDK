using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using MinGo.ServiceRegistry.Abstractions;

namespace MinGo.ServiceRegistry.ServiceDiscovery;

/// <summary>
/// Resolves a logical service name to concrete endpoints by querying the Registry Server through
/// <see cref="IServiceRegistryClient"/>.
/// </summary>
internal sealed class RegistryServiceEndpointProvider : IServiceEndpointProvider, IHostNameFeature, IAsyncDisposable
{
    private readonly ServiceEndpointQuery _query;
    private readonly IServiceRegistryClient _client;
    private readonly RegistryEndpointChangeTokenSource _changeTokens;
    private readonly ILogger _logger;

    public RegistryServiceEndpointProvider(
        ServiceEndpointQuery query,
        IServiceRegistryClient client,
        RegistryEndpointChangeTokenSource changeTokens,
        ILogger logger)
    {
        _query = query;
        _client = client;
        _changeTokens = changeTokens;
        _logger = logger;
    }

    /// <inheritdoc />
    public string HostName => _query.ServiceName;

    /// <inheritdoc />
    public async ValueTask PopulateAsync(IServiceEndpointBuilder endpoints, CancellationToken cancellationToken)
    {
        // Let a higher-priority provider win if it already produced endpoints.
        if (endpoints.Endpoints.Count > 0)
        {
            return;
        }

        var instances = await _client.DiscoverAsync(_query.ServiceName, cancellationToken).ConfigureAwait(false);

        foreach (var instance in instances)
        {
            if (!Uri.TryCreate($"{instance.Scheme}://{instance.Host}:{instance.Port}", UriKind.Absolute, out var uri))
            {
                _logger.LogWarning("Skipping instance {InstanceId} with invalid address {Scheme}://{Host}:{Port}.", instance.InstanceId, instance.Scheme, instance.Host, instance.Port);
                continue;
            }

            var features = new FeatureCollection();
            features.Set<IServiceEndpointProvider>(this);
            features.Set<IHostNameFeature>(this);

            endpoints.Endpoints.Add(ServiceEndpoint.Create(new UriEndPoint(uri), features));
        }

        endpoints.AddChangeToken(_changeTokens.GetChangeToken());

        _logger.LogDebug("Resolved {Count} endpoint(s) for service {ServiceName}.", instances.Count, _query.ServiceName);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => default;
}
