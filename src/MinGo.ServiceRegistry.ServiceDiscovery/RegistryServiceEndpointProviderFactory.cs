using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using MinGo.ServiceRegistry.Abstractions;

namespace MinGo.ServiceRegistry.ServiceDiscovery;

/// <summary>
/// Creates <see cref="RegistryServiceEndpointProvider"/> instances for Microsoft Service Discovery queries.
/// </summary>
internal sealed class RegistryServiceEndpointProviderFactory : IServiceEndpointProviderFactory
{
    private readonly IServiceRegistryClient _client;
    private readonly RegistryEndpointChangeTokenSource _changeTokens;
    private readonly ILoggerFactory _loggerFactory;

    public RegistryServiceEndpointProviderFactory(
        IServiceRegistryClient client,
        RegistryEndpointChangeTokenSource changeTokens,
        ILoggerFactory loggerFactory)
    {
        _client = client;
        _changeTokens = changeTokens;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public bool TryCreateProvider(ServiceEndpointQuery query, out IServiceEndpointProvider provider)
    {
        provider = new RegistryServiceEndpointProvider(
            query,
            _client,
            _changeTokens,
            _loggerFactory.CreateLogger<RegistryServiceEndpointProvider>());

        return true;
    }
}
