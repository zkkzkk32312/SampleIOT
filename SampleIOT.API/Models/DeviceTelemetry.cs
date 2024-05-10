using System;

namespace SampleIOT.API.Models
{
    [Serializable]
    public class DeviceTelemetry
    {
        public Device Device { get; set; }

        public Telemetry[] Telemetries { get; set; }
    }
}
