using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using MinGo.ServiceRegistry.Abstractions;

namespace MinGo.ServiceRegistry.Client;

/// <summary>
/// Default <see cref="IServiceRegistryClient"/> implementation talking to the Registry Server over HTTP.
/// </summary>
/// <remarks>
/// This type is registered as a typed <see cref="HttpClient"/>; resilience (retry / circuit breaker)
/// is expected to be layered on via <c>Microsoft.Extensions.Http.Resilience</c>.
/// </remarks>
public sealed class RegistryClient : IServiceRegistryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RegistryClient> _logger;

    /// <summary>Initializes a new instance of <see cref="RegistryClient"/>.</summary>
    public RegistryClient(HttpClient httpClient, ILogger<RegistryClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RegistrationResult> RegisterAsync(ServiceRegistration registration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var url = $"api/registry/services/{Uri.EscapeDataString(registration.ServiceName)}/instances";
        using var response = await _httpClient
            .PostAsJsonAsync(url, registration, RegistryJsonContext.Default.ServiceRegistration, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var result = await response.Content
            .ReadFromJsonAsync(RegistryJsonContext.Default.RegistrationResult, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new RegistryException("Registration response body was empty.");
    }

    /// <inheritdoc />
    public async Task<RenewResult> RenewLeaseAsync(string leaseId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseId);

        var url = $"api/registry/leases/{Uri.EscapeDataString(leaseId)}";
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new RegistryLeaseLostException { StatusCode = (int)HttpStatusCode.NotFound };
        }

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var result = await response.Content
            .ReadFromJsonAsync(RegistryJsonContext.Default.RenewResult, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new RegistryException("Renew response body was empty.");
    }

    /// <inheritdoc />
    public async Task DeregisterAsync(string leaseId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseId);

        var url = $"api/registry/leases/{Uri.EscapeDataString(leaseId)}";
        using var response = await _httpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

        // A missing lease means the instance is already gone - treat deregister as idempotent.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceInstance>> DiscoverAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var url = $"api/registry/services/{Uri.EscapeDataString(serviceName)}/instances";
        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var payload = await response.Content
            .ReadFromJsonAsync(RegistryJsonContext.Default.ServiceInstancesResponse, cancellationToken)
            .ConfigureAwait(false);

        return payload?.Instances ?? [];
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = string.Empty;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Unable to read error response body.");
        }

        _logger.LogError(
            "Registry request to {Uri} failed with status {StatusCode}: {Body}",
            response.RequestMessage?.RequestUri,
            (int)response.StatusCode,
            body);

        throw new RegistryException($"Registry request failed with status {(int)response.StatusCode}.")
        {
            StatusCode = (int)response.StatusCode,
        };
    }
}
