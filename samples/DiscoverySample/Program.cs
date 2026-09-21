// Discovery sample: this service consumes another service by logical name.
// Endpoints are resolved through the Registry Server via Microsoft Service Discovery,
// so the downstream HttpClient only knows a logical name (http://<service>), never a host:port.
var builder = WebApplication.CreateBuilder(args);

var registryEndpoint = new Uri(builder.Configuration["RegistryEndpoint"] ?? "http://localhost:5080");
var downstreamService = builder.Configuration["DownstreamService"] ?? "registration-sample";

// Talks to the Registry Server to look up instances.
builder.Services.AddServiceRegistryClient(o => o.RegistryEndpoint = registryEndpoint);

// Resolves logical service names (http://<name>) via the Registry Server.
builder.Services.AddRegistryServiceDiscovery();

// A downstream HttpClient addressed by logical name; AddServiceDiscovery() rewrites it
// to a concrete endpoint discovered from the Registry Server.
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
