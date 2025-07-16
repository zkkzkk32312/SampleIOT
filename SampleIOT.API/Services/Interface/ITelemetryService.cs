using SampleIOT.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleIOT.API.Services.Interface
{
    public interface ITelemetryService
    {
        DeviceTelemetry GetTelemetry(string DeviceId);
        Action<string, Telemetry> NewTelemetryReceived { get; set; }
        Task Start();
    }
}
