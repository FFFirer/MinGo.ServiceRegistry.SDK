using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MinGo.ServiceRegistry.Abstractions;
using MinGo.ServiceRegistry.Client;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency-injection extensions for the Registry client.
/// </summary>
public static class ServiceRegistryClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IServiceRegistryClient"/> (backed by <see cref="RegistryClient"/>)
    /// as a typed <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration of <see cref="ServiceRegistryClientOptions"/>.</param>
    public static IServiceCollection AddServiceRegistryClient(
        this IServiceCollection services,
        Action<ServiceRegistryClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ServiceRegistryClientOptions>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.AddHttpClient<RegistryClient>((sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<ServiceRegistryClientOptions>>().Value;
            if (options.RegistryEndpoint is not null)
            {
                httpClient.BaseAddress = options.RegistryEndpoint;
            }

            httpClient.Timeout = options.RequestTimeout;
        });

        services.TryAddTransient<IServiceRegistryClient>(sp => sp.GetRequiredService<RegistryClient>());

        return services;
    }
}
