using System;

namespace SampleIOT.API.Models
{
    [Serializable]
    public class Telemetry
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
