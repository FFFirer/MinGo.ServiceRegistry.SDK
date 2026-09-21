namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// The result returned by the server after a successful registration. Carries the
/// server-issued <see cref="LeaseId"/> that the client must use to renew and deregister.
/// </summary>
public sealed class RegistrationResult
{
    /// <summary>Server-issued lease identifier. Use it for renew and deregister calls.</summary>
    public string LeaseId { get; init; } = string.Empty;

    /// <summary>The effective instance identifier (client-supplied or server-generated).</summary>
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>UTC instant at which the lease expires if not renewed.</summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>The effective lease time-to-live, in seconds.</summary>
    public int TtlSeconds { get; init; }
}
