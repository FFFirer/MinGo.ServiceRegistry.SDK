using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MinGo.ServiceRegistry.Abstractions;
using MinGo.ServiceRegistry.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency-injection extensions for registering the current service with the Registry Server.
/// </summary>
public static class ServiceRegistryServiceCollectionExtensions
{
    /// <summary>
    /// Adds automatic service registration, heartbeat and deregistration to the host, backed by
    /// <see cref="IServiceRegistryClient"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration for <see cref="ServiceRegistryOptions"/>.</param>
    public static IServiceCollection AddServiceRegistry(
        this IServiceCollection services,
        Action<ServiceRegistryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ServiceRegistryOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        // Register the protocol client (IServiceRegistryClient) and map registration options onto client options.
        services.AddServiceRegistryClient();
        services.AddOptions<ServiceRegistryClientOptions>()
            .Configure<IOptions<ServiceRegistryOptions>>((client, registration) =>
            {
                client.RegistryEndpoint = registration.Value.RegistryEndpoint;
                client.RequestTimeout = registration.Value.RequestTimeout;
            });

        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<ServiceRegistrationHostedService>();

        return services;
    }
}
