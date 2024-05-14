using SampleIOT.API.Models;
using System;
using System.Collections.Generic;

namespace SampleIOT.API.Services.Interface
{
    public interface ITelemetryService
    {
        DeviceTelemetry GetTelemetry(string DeviceId);
        Action<string, Telemetry> NewTelemetryReceived { get; set; }
        void Start();
    }
}
