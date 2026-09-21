namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// Options controlling how <see cref="IServiceRegistryClient"/> connects to the Registry Server.
/// </summary>
public sealed class ServiceRegistryClientOptions
{
    /// <summary>Base address of the Registry Server, e.g. <c>https://registry:8500</c>.</summary>
    public Uri? RegistryEndpoint { get; set; }

    /// <summary>Per-request timeout applied to the underlying <see cref="System.Net.Http.HttpClient"/>.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
