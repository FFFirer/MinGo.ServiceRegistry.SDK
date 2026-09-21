using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery;
using MinGo.ServiceRegistry.Abstractions;
using Xunit;

namespace MinGo.ServiceRegistry.ServiceDiscovery.Tests;

public sealed class ServiceDiscoveryDependencyInjectionTests
{
    [Fact]
    public void AddRegistryServiceDiscovery_registers_registry_provider_factory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceRegistryClient>(new FakeDiscoveryClient());

        services.AddRegistryServiceDiscovery();

        Assert.Contains(
            services,
            d => d.ServiceType == typeof(IServiceEndpointProviderFactory)
                 && d.ImplementationType == typeof(RegistryServiceEndpointProviderFactory));
    }
}
