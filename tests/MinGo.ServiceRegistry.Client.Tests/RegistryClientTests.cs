using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MinGo.ServiceRegistry.Abstractions;
using Xunit;

namespace MinGo.ServiceRegistry.Client.Tests;

public sealed class RegistryClientTests
{
    private static readonly Uri BaseAddress = new("http://registry/");

    private static (RegistryClient client, StubHttpMessageHandler handler) CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = BaseAddress };
        var client = new RegistryClient(http, NullLogger<RegistryClient>.Instance);
        return (client, handler);
    }

    private static ServiceRegistration Registration() =>
        new()
        {
            ServiceName = "svc",
            InstanceId = "i1",
            Scheme = "http",
            Host = "localhost",
            Port = 5000,
        };

    [Fact]
    public async Task RegisterAsync_posts_to_instances_route_and_returns_result()
    {
        var payload = new RegistrationResult
        {
            LeaseId = "lease-1",
            InstanceId = "i1",
            ExpiresAt = new DateTimeOffset(2024, 1, 1, 0, 0, 15, TimeSpan.Zero),
            TtlSeconds = 15,
        };
        var (client, handler) = CreateClient(_ =>
            StubHttpMessageHandler.Json(HttpStatusCode.Created, JsonSerializer.Serialize(payload, RegistryJsonContext.Default.RegistrationResult)));

        var result = await client.RegisterAsync(Registration());

        Assert.Equal("lease-1", result.LeaseId);
        Assert.Equal("i1", result.InstanceId);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://registry/api/registry/services/svc/instances", request.RequestUri!.ToString());
        Assert.Contains("\"serviceName\":\"svc\"", handler.RequestBodies[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_throws_with_status_on_error_response()
    {
        var (client, _) = CreateClient(_ => StubHttpMessageHandler.Json(HttpStatusCode.BadRequest, "{\"title\":\"bad\"}"));

        var ex = await Assert.ThrowsAsync<RegistryException>(() => client.RegisterAsync(Registration()));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task RenewLeaseAsync_puts_to_lease_route_and_returns_result()
    {
        var payload = new RenewResult
        {
            LeaseId = "lease-1",
            ExpiresAt = new DateTimeOffset(2024, 1, 1, 0, 0, 30, TimeSpan.Zero),
        };
        var (client, handler) = CreateClient(_ =>
            StubHttpMessageHandler.Json(HttpStatusCode.OK, JsonSerializer.Serialize(payload, RegistryJsonContext.Default.RenewResult)));

        var result = await client.RenewLeaseAsync("lease-1");

        Assert.Equal("lease-1", result.LeaseId);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("http://registry/api/registry/leases/lease-1", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task RenewLeaseAsync_throws_lease_lost_on_404()
    {
        var (client, _) = CreateClient(_ => StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<RegistryLeaseLostException>(() => client.RenewLeaseAsync("gone"));
    }

    [Fact]
    public async Task DeregisterAsync_deletes_lease_route()
    {
        var (client, handler) = CreateClient(_ => StubHttpMessageHandler.Empty(HttpStatusCode.NoContent));

        await client.DeregisterAsync("lease-1");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("http://registry/api/registry/leases/lease-1", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task DeregisterAsync_is_idempotent_on_404()
    {
        var (client, _) = CreateClient(_ => StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));

        // Must not throw.
        await client.DeregisterAsync("gone");
    }

    [Fact]
    public async Task DeregisterAsync_throws_on_server_error()
    {
        var (client, _) = CreateClient(_ => StubHttpMessageHandler.Empty(HttpStatusCode.InternalServerError));

        var ex = await Assert.ThrowsAsync<RegistryException>(() => client.DeregisterAsync("lease-1"));

        Assert.Equal(500, ex.StatusCode);
    }

    [Fact]
    public async Task DiscoverAsync_gets_instances_route_and_returns_instances()
    {
        var payload = new ServiceInstancesResponse
        {
            ServiceName = "svc",
            Instances =
            [
                new ServiceInstance { ServiceName = "svc", InstanceId = "i1", Scheme = "http", Host = "localhost", Port = 5000 },
            ],
        };
        var (client, handler) = CreateClient(_ =>
            StubHttpMessageHandler.Json(HttpStatusCode.OK, JsonSerializer.Serialize(payload, RegistryJsonContext.Default.ServiceInstancesResponse)));

        var instances = await client.DiscoverAsync("svc");

        var instance = Assert.Single(instances);
        Assert.Equal("i1", instance.InstanceId);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("http://registry/api/registry/services/svc/instances", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task DiscoverAsync_returns_empty_when_no_instances()
    {
        var payload = new ServiceInstancesResponse { ServiceName = "svc" };
        var (client, _) = CreateClient(_ =>
            StubHttpMessageHandler.Json(HttpStatusCode.OK, JsonSerializer.Serialize(payload, RegistryJsonContext.Default.ServiceInstancesResponse)));

        var instances = await client.DiscoverAsync("svc");

        Assert.Empty(instances);
    }
}
