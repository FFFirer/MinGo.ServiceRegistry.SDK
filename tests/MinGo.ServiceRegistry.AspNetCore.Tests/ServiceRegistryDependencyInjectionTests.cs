using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinGo.ServiceRegistry.Abstractions;
using Xunit;

namespace MinGo.ServiceRegistry.AspNetCore.Tests;

public sealed class ServiceRegistryDependencyInjectionTests
{
    [Fact]
    public void AddServiceRegistry_registers_hosted_service_and_client()
    {
        var services = new ServiceCollection();

        services.AddServiceRegistry(o =>
        {
            o.ServiceName = "svc";
            o.RegistryEndpoint = new Uri("http://registry/");
        });

        Assert.Contains(services, d => d.ServiceType == typeof(IHostedService));

        using var provider = services.BuildServiceProvider();
        Assert.IsAssignableFrom<IServiceRegistryClient>(provider.GetRequiredService<IServiceRegistryClient>());
    }

    [Fact]
    public void AddServiceRegistry_flows_endpoint_into_client_options()
    {
        var services = new ServiceCollection();

        services.AddServiceRegistry(o =>
        {
            o.ServiceName = "svc";
            o.RegistryEndpoint = new Uri("http://registry:7000/");
            o.RequestTimeout = TimeSpan.FromSeconds(4);
        });

        using var provider = services.BuildServiceProvider();
        var clientOptions = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<ServiceRegistryClientOptions>>()
            .Value;

        Assert.Equal(new Uri("http://registry:7000/"), clientOptions.RegistryEndpoint);
        Assert.Equal(TimeSpan.FromSeconds(4), clientOptions.RequestTimeout);
    }
}
