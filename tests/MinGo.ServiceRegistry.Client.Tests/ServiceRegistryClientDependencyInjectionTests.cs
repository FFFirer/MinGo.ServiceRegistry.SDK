using Microsoft.Extensions.DependencyInjection;
using MinGo.ServiceRegistry.Abstractions;
using Xunit;

namespace MinGo.ServiceRegistry.Client.Tests;

public sealed class ServiceRegistryClientDependencyInjectionTests
{
    [Fact]
    public void AddServiceRegistryClient_registers_resolvable_client()
    {
        var services = new ServiceCollection();

        services.AddServiceRegistryClient(o => o.RegistryEndpoint = new Uri("http://registry/"));
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IServiceRegistryClient>();

        Assert.IsType<RegistryClient>(client);
    }

    [Fact]
    public void AddServiceRegistryClient_applies_options()
    {
        var services = new ServiceCollection();

        services.AddServiceRegistryClient(o =>
        {
            o.RegistryEndpoint = new Uri("http://registry:9000/");
            o.RequestTimeout = TimeSpan.FromSeconds(3);
        });
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServiceRegistryClientOptions>>().Value;

        Assert.Equal(new Uri("http://registry:9000/"), options.RegistryEndpoint);
        Assert.Equal(TimeSpan.FromSeconds(3), options.RequestTimeout);
    }
}
