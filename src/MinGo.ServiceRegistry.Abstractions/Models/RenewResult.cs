namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// The result returned by the server after a successful lease renewal.
/// </summary>
public sealed class RenewResult
{
    /// <summary>The lease identifier that was renewed.</summary>
    public string LeaseId { get; init; } = string.Empty;

    /// <summary>The new UTC expiry instant of the lease.</summary>
    public DateTimeOffset ExpiresAt { get; init; }
}
