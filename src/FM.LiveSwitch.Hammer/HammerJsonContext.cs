using System.Text.Json.Serialization;

namespace FM.LiveSwitch.Hammer
{
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(ScanTestOutput))]
    [JsonSerializable(typeof(MediaServerInfo[]))]
    [JsonSerializable(typeof(DeploymentConfig))]
    internal partial class HammerJsonContext : JsonSerializerContext
    {
    }
}
