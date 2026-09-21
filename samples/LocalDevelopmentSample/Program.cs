// Local-development sample: the SAME downstream HttpClient code as DiscoverySample, but
// endpoints resolve from configuration (appsettings.json "Services") instead of the Registry
// Server. This lets a developer run the service with no Registry dependency; swapping to the
// Registry-backed provider in production is a one-line DI change and no business-code change.
var builder = WebApplication.CreateBuilder(args);

var downstreamService = builder.Configuration["DownstreamService"] ?? "registration-sample";

// Official configuration/pass-through providers (no Registry Server needed).
builder.Services.AddServiceDiscovery();

// Identical downstream wiring to the Registry-backed DiscoverySample.
builder.Services.AddHttpClient("downstream", c => c.BaseAddress = new Uri($"http://{downstreamService}"))
    .AddServiceDiscovery();

var app = builder.Build();

app.MapGet("/", async (IHttpClientFactory factory, CancellationToken cancellationToken) =>
{
    var client = factory.CreateClient("downstream");
    var body = await client.GetStringAsync("/", cancellationToken);
    return Results.Ok(new { downstream = downstreamService, response = body });
});

app.MapGet("/health", () => Results.Ok());

app.Run();
