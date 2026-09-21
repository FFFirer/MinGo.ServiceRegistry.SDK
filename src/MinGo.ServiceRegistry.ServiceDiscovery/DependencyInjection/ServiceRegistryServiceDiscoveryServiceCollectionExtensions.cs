using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ServiceDiscovery;
using MinGo.ServiceRegistry.Abstractions;
using MinGo.ServiceRegistry.ServiceDiscovery;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency-injection extensions that plug the Registry Server into Microsoft Service Discovery.
/// </summary>
public static class ServiceRegistryServiceDiscoveryServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Registry-backed <see cref="IServiceEndpointProviderFactory"/> so that logical
    /// service names (e.g. <c>https://user-service</c>) resolve via the Registry Server.
    /// </summary>
    /// <remarks>
    /// An <see cref="IServiceRegistryClient"/> must also be registered (for example via
    /// <c>AddServiceRegistryClient(...)</c> or <c>AddServiceRegistry(...)</c>). For local development
    /// use <c>AddServiceDiscovery()</c> (configuration/pass-through providers) instead; the business
    /// <c>HttpClient</c> code is identical in both cases.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration of <see cref="ServiceRegistryServiceDiscoveryOptions"/>.</param>
    public static IServiceCollection AddRegistryServiceDiscovery(
        this IServiceCollection services,
        Action<ServiceRegistryServiceDiscoveryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddServiceDiscoveryCore();

        services.AddOptions<ServiceRegistryServiceDiscoveryOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<RegistryEndpointChangeTokenSource>();
        services.AddSingleton<IServiceEndpointProviderFactory, RegistryServiceEndpointProviderFactory>();

        return services;
    }
}
