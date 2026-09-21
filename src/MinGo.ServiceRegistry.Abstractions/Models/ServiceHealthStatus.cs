namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// Health / lifecycle state of a registered service instance.
/// </summary>
public enum ServiceHealthStatus
{
    /// <summary>The instance has been registered but has not yet been confirmed healthy.</summary>
    Registered = 0,

    /// <summary>The instance holds a valid, unexpired lease and can be discovered.</summary>
    Healthy = 1,

    /// <summary>The lease has expired; the instance is no longer discoverable and will be reaped.</summary>
    Expired = 2,
}
