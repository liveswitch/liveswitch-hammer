using System.Text.Json.Serialization;

namespace FM.LiveSwitch.Hammer
{
    [JsonConverter(typeof(JsonStringEnumConverter<ScanTestState>))]
    enum ScanTestState
    {
        Unknown,
        Skip,
        Pass,
        Fail
    }
}
