using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace MinGo.ServiceRegistry.ServiceDiscovery;

/// <summary>
/// Produces <see cref="IChangeToken"/> instances that signal (on a fixed polling interval) that
/// resolved registry endpoints should be refreshed. The MVP has no server push, so this drives
/// Microsoft Service Discovery to re-resolve periodically.
/// </summary>
internal sealed class RegistryEndpointChangeTokenSource : IDisposable
{
    private readonly object _gate = new();
    private readonly ITimer _timer;
    private CancellationTokenSource _cts = new();
    private bool _disposed;

    public RegistryEndpointChangeTokenSource(TimeProvider timeProvider, IOptions<ServiceRegistryServiceDiscoveryOptions> options)
    {
        var interval = options.Value.RefreshInterval;
        _timer = timeProvider.CreateTimer(static state => ((RegistryEndpointChangeTokenSource)state!).Rotate(), this, interval, interval);
    }

    public IChangeToken GetChangeToken()
    {
        lock (_gate)
        {
            return new CancellationChangeToken(_cts.Token);
        }
    }

    private void Rotate()
    {
        CancellationTokenSource previous;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            previous = _cts;
            _cts = new CancellationTokenSource();
        }

        // Running callbacks synchronously here is what triggers re-resolution in Service Discovery.
        previous.Cancel();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _timer.Dispose();
        _cts.Dispose();
    }
}
