namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// Base exception for Service Registry protocol failures.
/// </summary>
public class RegistryException : Exception
{
    /// <summary>Initializes a new instance of <see cref="RegistryException"/>.</summary>
    public RegistryException()
    {
    }

    /// <summary>Initializes a new instance of <see cref="RegistryException"/> with a message.</summary>
    public RegistryException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="RegistryException"/> with a message and inner exception.</summary>
    public RegistryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The HTTP status code observed, when the failure originated from a response.</summary>
    public int? StatusCode { get; init; }
}
