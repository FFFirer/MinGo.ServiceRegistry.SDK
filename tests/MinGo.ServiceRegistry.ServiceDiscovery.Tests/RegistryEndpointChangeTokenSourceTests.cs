using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace MinGo.ServiceRegistry.ServiceDiscovery.Tests;

public sealed class RegistryEndpointChangeTokenSourceTests
{
    [Fact]
    public void Token_fires_when_refresh_interval_elapses()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        using var source = new RegistryEndpointChangeTokenSource(
            time,
            Options.Create(new ServiceRegistryServiceDiscoveryOptions { RefreshInterval = TimeSpan.FromSeconds(15) }));

        var token = source.GetChangeToken();
        Assert.False(token.HasChanged);

        time.Advance(TimeSpan.FromSeconds(15));

        Assert.True(token.HasChanged);

        // A freshly-issued token reflects the rotated source and is not yet fired.
        var next = source.GetChangeToken();
        Assert.False(next.HasChanged);
    }
}
