using SampleIOT.API.Models;
using System.Collections.Generic;

namespace SampleIOT.API.Services.Interface
{
    public interface ITelemetryService
    {
        DeviceTelemetry GetTelemetry(string DeviceId);
        void Start();
    }
}
