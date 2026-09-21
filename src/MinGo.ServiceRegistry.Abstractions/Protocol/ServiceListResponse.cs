namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// Wire response for <c>GET /api/registry/services</c>.
/// </summary>
public sealed class ServiceListResponse
{
    /// <summary>The names of all services that currently have registered instances.</summary>
    public IReadOnlyList<string> Services { get; init; } = [];
}
