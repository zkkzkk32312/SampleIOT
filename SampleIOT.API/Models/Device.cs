using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SampleIOT.API.Models
{
    [Serializable]
    public class Device
    {
        [JsonPropertyName("DeviceId")]
        public string Id { get; set; }

        [JsonPropertyName("DeviceType")]
        public string Type { get; set; }

        [JsonPropertyName("TelemetryNames")]
        public string[] TelemetryNames { get; set; }

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}
