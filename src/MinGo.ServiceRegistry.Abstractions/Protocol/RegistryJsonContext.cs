using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinGo.ServiceRegistry.Abstractions;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for the Registry wire protocol.
/// Shared by the client and the server so both sides serialize identically
/// (camelCase, null-omitting) and remain AOT/trimming friendly.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ServiceRegistration))]
[JsonSerializable(typeof(RegistrationResult))]
[JsonSerializable(typeof(RenewResult))]
[JsonSerializable(typeof(ServiceInstance))]
[JsonSerializable(typeof(LeaseOptions))]
[JsonSerializable(typeof(ServiceInstancesResponse))]
[JsonSerializable(typeof(ServiceListResponse))]
public sealed partial class RegistryJsonContext : JsonSerializerContext
{
}
