using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MinGo.ServiceRegistry.Abstractions;
using Xunit;

namespace MinGo.ServiceRegistry.AspNetCore.Tests;

public sealed class ServiceRegistrationHostedServiceTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    private static (ServiceRegistrationHostedService service, FakeRegistryClient client, FakeTimeProvider time) Create(
        Action<ServiceRegistryOptions>? configure = null)
    {
        var client = new FakeRegistryClient();
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var options = new ServiceRegistryOptions
        {
            ServiceName = "svc",
            RegistryEndpoint = new Uri("http://registry/"),
            Scheme = "http",
            Host = "testhost",
            Port = 1234,
            LeaseTtlSeconds = 15,
            HeartbeatInterval = Interval,
        };
        configure?.Invoke(options);

        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = new ServiceRegistrationHostedService(
            client,
            Options.Create(options),
            time,
            NullLogger<ServiceRegistrationHostedService>.Instance,
            provider);
        return (service, client, time);
    }

    [Fact]
    public async Task Start_registers_with_advertised_endpoint()
    {
        var (service, client, _) = Create();

        await service.StartAsync(CancellationToken.None);
        await client.FirstRegistration.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, client.RegisterCount);
        Assert.NotNull(client.LastRegistration);
        Assert.Equal("svc", client.LastRegistration!.ServiceName);
        Assert.Equal("http", client.LastRegistration.Scheme);
        Assert.Equal("testhost", client.LastRegistration.Host);
        Assert.Equal(1234, client.LastRegistration.Port);
        Assert.Equal(15, client.LastRegistration.Lease?.TtlSeconds);
    }

    [Fact]
    public async Task Heartbeat_renews_the_lease_on_tick()
    {
        var (service, client, time) = Create();

        await service.StartAsync(CancellationToken.None);
        await client.FirstRegistration.Task.WaitAsync(TimeSpan.FromSeconds(5));

        time.Advance(Interval);
        await client.FirstRenewal.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.True(client.RenewCount >= 1);
    }

    [Fact]
    public async Task Lost_lease_triggers_reregistration()
    {
        var (service, client, time) = Create();
        client.ThrowLeaseLostOnRenew = true;

        await service.StartAsync(CancellationToken.None);
        await client.FirstRegistration.Task.WaitAsync(TimeSpan.FromSeconds(5));

        time.Advance(Interval);
        await client.Reregistration.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.True(client.RegisterCount >= 2);
    }

    [Fact]
    public async Task Stop_deregisters_the_active_lease()
    {
        var (service, client, _) = Create();
        await service.StartAsync(CancellationToken.None);
        await client.FirstRegistration.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, client.DeregisterCount);
        Assert.Equal("lease-1", client.DeregisteredLeaseId);
    }

    [Fact]
    public async Task DeregisterOnStop_false_skips_deregistration()
    {
        var (service, client, _) = Create(o => o.DeregisterOnStop = false);
        await service.StartAsync(CancellationToken.None);
        await client.FirstRegistration.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, client.DeregisterCount);
    }

    [Fact]
    public async Task RegisterOnStart_false_does_not_register()
    {
        var (service, client, _) = Create(o => o.RegisterOnStart = false);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, client.RegisterCount);
        Assert.Equal(0, client.DeregisterCount);
    }

    [Fact]
    public async Task Missing_service_name_throws_on_start()
    {
        var (service, _, _) = Create(o => o.ServiceName = string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(CancellationToken.None));
    }
}
