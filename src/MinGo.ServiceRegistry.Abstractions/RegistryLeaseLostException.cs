namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// Thrown when the server no longer recognizes a lease (HTTP 404). Callers should treat this
/// as a signal to re-register the instance rather than as a fatal error.
/// </summary>
public sealed class RegistryLeaseLostException : RegistryException
{
    /// <summary>Initializes a new instance of <see cref="RegistryLeaseLostException"/>.</summary>
    public RegistryLeaseLostException()
        : base("The registry lease no longer exists; the instance must re-register.")
    {
    }

    /// <summary>Initializes a new instance of <see cref="RegistryLeaseLostException"/> with a message.</summary>
    public RegistryLeaseLostException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="RegistryLeaseLostException"/> with a message and inner exception.</summary>
    public RegistryLeaseLostException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
