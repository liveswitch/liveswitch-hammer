using System.Text.Json.Serialization;

namespace FM.LiveSwitch.Hammer
{
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UseStringEnumConverter = true,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(ScanTestOutput))]
    [JsonSerializable(typeof(MediaServerInfo[]))]
    [JsonSerializable(typeof(DeploymentConfig))]
    // Add [JsonSerializable(typeof(T))] for each type that needs STJ serialization/deserialization.
    internal partial class HammerJsonContext : JsonSerializerContext
    {
    }
}
