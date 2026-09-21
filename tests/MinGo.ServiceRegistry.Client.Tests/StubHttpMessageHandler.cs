using System.Net;
using System.Text;

namespace MinGo.ServiceRegistry.Client.Tests;

/// <summary>
/// A minimal <see cref="HttpMessageHandler"/> that records requests and returns a canned response,
/// so <see cref="RegistryClient"/> can be exercised without a network.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responder = responder;

    /// <summary>All requests sent through the handler, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>The captured request bodies (null when a request had no content), in order.</summary>
    public List<string?> RequestBodies { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        return _responder(request);
    }

    /// <summary>Builds a JSON response with the given status.</summary>
    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>Builds an empty response with the given status.</summary>
    public static HttpResponseMessage Empty(HttpStatusCode status) => new(status);
}
