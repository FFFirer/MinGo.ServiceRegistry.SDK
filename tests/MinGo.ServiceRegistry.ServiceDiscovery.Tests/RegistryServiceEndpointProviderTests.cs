using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.Extensions.Time.Testing;
using MinGo.ServiceRegistry.Abstractions;
using Xunit;

namespace MinGo.ServiceRegistry.ServiceDiscovery.Tests;

public sealed class RegistryServiceEndpointProviderTests
{
    private static ServiceEndpointQuery Query(string serviceName)
    {
        Assert.True(ServiceEndpointQuery.TryParse($"http://{serviceName}", out var query), "query should parse");
        return query!;
    }

    private static ServiceInstance Instance(string host, int port, string scheme = "http") =>
        new()
        {
            ServiceName = "user-service",
            InstanceId = $"{host}:{port}",
            Scheme = scheme,
            Host = host,
            Port = port,
            Health = ServiceHealthStatus.Healthy,
        };

    private static (RegistryServiceEndpointProviderFactory factory, FakeDiscoveryClient client) CreateFactory(
        params ServiceInstance[] instances)
    {
        var client = new FakeDiscoveryClient(instances);
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var changeTokens = new RegistryEndpointChangeTokenSource(
            time,
            Options.Create(new ServiceRegistryServiceDiscoveryOptions { RefreshInterval = TimeSpan.FromSeconds(15) }));
        var factory = new RegistryServiceEndpointProviderFactory(client, changeTokens, NullLoggerFactory.Instance);
        return (factory, client);
    }

    [Fact]
    public void TryCreateProvider_returns_registry_provider()
    {
        var (factory, _) = CreateFactory();

        var created = factory.TryCreateProvider(Query("user-service"), out var provider);

        Assert.True(created);
        Assert.IsType<RegistryServiceEndpointProvider>(provider);
    }

    [Fact]
    public async Task PopulateAsync_adds_an_endpoint_per_instance_and_a_change_token()
    {
        var (factory, client) = CreateFactory(Instance("10.0.0.1", 5001), Instance("10.0.0.2", 5002));
        factory.TryCreateProvider(Query("user-service"), out var provider);
        var builder = new TestEndpointBuilder();

        await provider.PopulateAsync(builder, CancellationToken.None);

        Assert.Equal(2, builder.Endpoints.Count);
        var first = Assert.IsType<UriEndPoint>(builder.Endpoints[0].EndPoint);
        Assert.Equal(new Uri("http://10.0.0.1:5001"), first.Uri);
        var second = Assert.IsType<UriEndPoint>(builder.Endpoints[1].EndPoint);
        Assert.Equal(new Uri("http://10.0.0.2:5002"), second.Uri);
        Assert.NotEmpty(builder.ChangeTokens);
        Assert.Equal(new[] { "user-service" }, client.DiscoveredServices);
    }

    [Fact]
    public async Task PopulateAsync_exposes_host_name_feature()
    {
        var (factory, _) = CreateFactory(Instance("10.0.0.1", 5001));
        factory.TryCreateProvider(Query("user-service"), out var provider);
        var builder = new TestEndpointBuilder();

        await provider.PopulateAsync(builder, CancellationToken.None);

        var hostNameFeature = builder.Endpoints[0].Features.Get<IHostNameFeature>();
        Assert.NotNull(hostNameFeature);
        Assert.Equal("user-service", hostNameFeature!.HostName);
    }

    [Fact]
    public async Task PopulateAsync_yields_to_an_existing_higher_priority_provider()
    {
        var (factory, client) = CreateFactory(Instance("10.0.0.1", 5001));
        factory.TryCreateProvider(Query("user-service"), out var provider);
        var builder = new TestEndpointBuilder();
        builder.Endpoints.Add(ServiceEndpoint.Create(new UriEndPoint(new Uri("http://preset:1")), new FeatureCollection()));

        await provider.PopulateAsync(builder, CancellationToken.None);

        Assert.Single(builder.Endpoints);
        Assert.Empty(client.DiscoveredServices);
    }

    [Fact]
    public async Task PopulateAsync_skips_instances_with_invalid_addresses()
    {
        var (factory, _) = CreateFactory(Instance("10.0.0.1", 5001), Instance("10.0.0.9", 99999));
        factory.TryCreateProvider(Query("user-service"), out var provider);
        var builder = new TestEndpointBuilder();

        await provider.PopulateAsync(builder, CancellationToken.None);

        var endpoint = Assert.Single(builder.Endpoints);
        var uri = Assert.IsType<UriEndPoint>(endpoint.EndPoint);
        Assert.Equal(new Uri("http://10.0.0.1:5001"), uri.Uri);
    }

    [Fact]
    public async Task PopulateAsync_with_no_instances_adds_only_change_token()
    {
        var (factory, _) = CreateFactory();
        factory.TryCreateProvider(Query("user-service"), out var provider);
        var builder = new TestEndpointBuilder();

        await provider.PopulateAsync(builder, CancellationToken.None);

        Assert.Empty(builder.Endpoints);
        Assert.NotEmpty(builder.ChangeTokens);
    }
}
