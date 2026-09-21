// Registration sample: this service registers itself with the Registry Server on start,
// keeps its lease alive with heartbeats, and deregisters on graceful shutdown.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceRegistry(options =>
{
    options.ServiceName = builder.Configuration["ServiceName"] ?? "registration-sample";
    options.RegistryEndpoint = new Uri(builder.Configuration["RegistryEndpoint"] ?? "http://localhost:5080");
    options.LeaseTtlSeconds = 15;
    options.Metadata["sample"] = "registration";
});

var app = builder.Build();

app.MapGet("/", () => "Hello from the MinGo Service Registry registration sample!");
app.MapGet("/health", () => Results.Ok());

app.Run();
