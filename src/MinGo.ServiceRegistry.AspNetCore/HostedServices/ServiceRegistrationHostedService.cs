using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.ServiceRegistry.Abstractions;

namespace MinGo.ServiceRegistry.AspNetCore;

/// <summary>
/// Registers the current service on host start, keeps the lease alive with periodic heartbeats and
/// deregisters on host stop. Re-registers automatically when the lease is lost (server restarted / expired).
/// </summary>
internal sealed class ServiceRegistrationHostedService : IHostedService
{
    private readonly IServiceRegistryClient _client;
    private readonly ServiceRegistryOptions _options;
    private readonly IServer? _server;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ServiceRegistrationHostedService> _logger;

    private readonly object _gate = new();
    private string? _leaseId;
    private string? _instanceId;
    private CancellationTokenSource? _cts;
    private Task? _heartbeatTask;

    public ServiceRegistrationHostedService(
        IServiceRegistryClient client,
        IOptions<ServiceRegistryOptions> options,
        TimeProvider timeProvider,
        ILogger<ServiceRegistrationHostedService> logger,
        IServiceProvider services)
    {
        _client = client;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
        _server = services.GetService<IServer>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.RegisterOnStart)
        {
            _logger.LogInformation("Service registration disabled (RegisterOnStart=false).");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(_options.ServiceName))
        {
            throw new InvalidOperationException("ServiceRegistryOptions.ServiceName must be set to register with the Registry Server.");
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var interval = _options.HeartbeatInterval ?? TimeSpan.FromSeconds(Math.Max(1, _options.LeaseTtlSeconds / 3));

        // The loop performs the initial registration and all subsequent heartbeats/re-registrations,
        // so a Registry outage at startup does not prevent the host from starting.
        _heartbeatTask = RunHeartbeatLoopAsync(interval, _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }

        if (_heartbeatTask is not null)
        {
            try
            {
                await _heartbeatTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }

        string? leaseId;
        lock (_gate)
        {
            leaseId = _leaseId;
        }

        if (_options.DeregisterOnStop && leaseId is not null)
        {
            try
            {
                await _client.DeregisterAsync(leaseId, CancellationToken.None).ConfigureAwait(false);
                _logger.LogInformation("Deregistered instance {InstanceId} (lease {LeaseId}).", _instanceId, leaseId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Best-effort deregistration failed for lease {LeaseId}.", leaseId);
            }
        }

        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunHeartbeatLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval, _timeProvider);
        var consecutiveFailures = 0;

        try
        {
            // Attempt an immediate registration, then rely on the timer for heartbeats.
            await RegisterOrRenewAsync(cancellationToken).ConfigureAwait(false);

            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await RegisterOrRenewAsync(cancellationToken).ConfigureAwait(false);
                    consecutiveFailures = 0;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    consecutiveFailures++;
                    _logger.LogError(ex, "Heartbeat failed ({Count} consecutive).", consecutiveFailures);

                    var backoffSeconds = Math.Min(30, Math.Pow(2, consecutiveFailures));
                    await DelayAsync(TimeSpan.FromSeconds(backoffSeconds), cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path.
        }
    }

    private async Task RegisterOrRenewAsync(CancellationToken cancellationToken)
    {
        string? leaseId;
        lock (_gate)
        {
            leaseId = _leaseId;
        }

        if (leaseId is null)
        {
            await RegisterAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            await _client.RenewLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);
        }
        catch (RegistryLeaseLostException)
        {
            _logger.LogWarning("Lease {LeaseId} was lost; re-registering.", leaseId);
            lock (_gate)
            {
                _leaseId = null;
            }

            await RegisterAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        var (scheme, host, port) = ResolveEndpoint();

        string? instanceId;
        lock (_gate)
        {
            instanceId = _options.InstanceId ?? _instanceId;
        }

        var registration = new ServiceRegistration
        {
            ServiceName = _options.ServiceName,
            InstanceId = instanceId,
            Scheme = scheme,
            Host = host,
            Port = port,
            Metadata = _options.Metadata.Count > 0 ? new Dictionary<string, string>(_options.Metadata) : null,
            Lease = new LeaseOptions { TtlSeconds = _options.LeaseTtlSeconds },
        };

        var result = await _client.RegisterAsync(registration, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            _leaseId = result.LeaseId;
            _instanceId = result.InstanceId;
        }

        _logger.LogInformation(
            "Registered {ServiceName}/{InstanceId} at {Scheme}://{Host}:{Port} (lease {LeaseId}, ttl {Ttl}s).",
            _options.ServiceName,
            result.InstanceId,
            scheme,
            host,
            port,
            result.LeaseId,
            result.TtlSeconds);
    }

    private (string Scheme, string Host, int Port) ResolveEndpoint()
    {
        var scheme = _options.Scheme;
        var host = _options.Host;
        var port = _options.Port;

        if (host is null || port is null || scheme is null)
        {
            var addresses = _server?.Features.Get<IServerAddressesFeature>()?.Addresses;
            var firstAddress = addresses?.FirstOrDefault();
            if (firstAddress is not null && Uri.TryCreate(firstAddress, UriKind.Absolute, out var bound))
            {
                scheme ??= bound.Scheme;
                host ??= bound.Host is "localhost" or "+" or "*" ? Dns.GetHostName() : bound.Host;
                port ??= bound.Port;
            }
        }

        return (
            scheme ?? ServiceRegistryDefaults.DefaultScheme,
            host ?? Dns.GetHostName(),
            port ?? 0);
    }

    private async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        using var timer = _timeProvider.CreateTimer(static state => ((TaskCompletionSource)state!).TrySetResult(), tcs, delay, Timeout.InfiniteTimeSpan);
        try
        {
            await tcs.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Rethrown to the caller's cancellation handling.
            throw;
        }
    }
}
