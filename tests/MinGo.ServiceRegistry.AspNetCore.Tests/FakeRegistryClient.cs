using MinGo.ServiceRegistry.Abstractions;

namespace MinGo.ServiceRegistry.AspNetCore.Tests;

/// <summary>
/// In-memory <see cref="IServiceRegistryClient"/> used to observe the hosted service's lifecycle
/// without a real Registry Server.
/// </summary>
internal sealed class FakeRegistryClient : IServiceRegistryClient
{
    private int _registerCount;

    /// <summary>Number of completed registrations.</summary>
    public int RegisterCount => Volatile.Read(ref _registerCount);

    /// <summary>Number of renewal attempts (including ones that threw).</summary>
    public int RenewCount { get; private set; }

    /// <summary>Number of deregistrations.</summary>
    public int DeregisterCount { get; private set; }

    /// <summary>The lease id passed to the most recent deregistration.</summary>
    public string? DeregisteredLeaseId { get; private set; }

    /// <summary>The most recent registration payload.</summary>
    public ServiceRegistration? LastRegistration { get; private set; }

    /// <summary>When set, renewals throw <see cref="RegistryLeaseLostException"/> to force re-registration.</summary>
    public bool ThrowLeaseLostOnRenew { get; set; }

    /// <summary>Signalled once the first registration completes.</summary>
    public TaskCompletionSource FirstRegistration { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signalled once a second (re-)registration completes.</summary>
    public TaskCompletionSource Reregistration { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signalled once the first renewal completes.</summary>
    public TaskCompletionSource FirstRenewal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<RegistrationResult> RegisterAsync(ServiceRegistration registration, CancellationToken cancellationToken = default)
    {
        LastRegistration = registration;
        var count = Interlocked.Increment(ref _registerCount);
        var result = new RegistrationResult
        {
            LeaseId = $"lease-{count}",
            InstanceId = registration.InstanceId ?? $"generated-{count}",
            ExpiresAt = DateTimeOffset.UnixEpoch.AddSeconds(registration.Lease?.TtlSeconds ?? 15),
            TtlSeconds = registration.Lease?.TtlSeconds ?? 15,
        };

        if (count == 1)
        {
            FirstRegistration.TrySetResult();
        }
        else
        {
            Reregistration.TrySetResult();
        }

        return Task.FromResult(result);
    }

    public Task<RenewResult> RenewLeaseAsync(string leaseId, CancellationToken cancellationToken = default)
    {
        RenewCount++;
        if (ThrowLeaseLostOnRenew)
        {
            throw new RegistryLeaseLostException();
        }

        FirstRenewal.TrySetResult();
        return Task.FromResult(new RenewResult { LeaseId = leaseId, ExpiresAt = DateTimeOffset.UnixEpoch.AddSeconds(30) });
    }

    public Task DeregisterAsync(string leaseId, CancellationToken cancellationToken = default)
    {
        DeregisterCount++;
        DeregisteredLeaseId = leaseId;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ServiceInstance>> DiscoverAsync(string serviceName, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceInstance>>([]);
}
