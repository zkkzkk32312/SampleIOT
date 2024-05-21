using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Models;
using SampleIOT.API.Services.Interface;
using System.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SampleIOT.API.Controllers
{
    [Route("api/[controller]")]
    [Route("Telemetry")]
    [ApiController]
    public class TelemetryController : ControllerBase
    {
        private ITelemetryService telemetryService;
        private readonly ILogger<TelemetryController> _logger;
        public TelemetryController(ITelemetryService service, ILogger<TelemetryController> logger)
        {
            this.telemetryService = service;
            this._logger = logger;
        }

        // GET api/<TelemetryController>/5
        [HttpGet("{id}")]
        public IActionResult Get(string id, int?limit, bool? disaggregated)
        {
            var deviceTelemetry = telemetryService.GetTelemetry(id);

            if (deviceTelemetry == null)
                return NotFound();

            if (limit.HasValue && limit.Value < 0)
                return BadRequest();

            if (disaggregated != null && disaggregated == true)
            {
                var disaggregatedDeviceTelemetry = deviceTelemetry
                    .Telemetries
                    .GroupBy(x => x.Key)
                    .Select(y => new DeviceTelemetry
                    {
                        Device = deviceTelemetry.Device,
                        Telemetries = limit.HasValue ? y.TakeLast(limit.Value).ToArray() : y.ToArray()
                    })
                    .ToList();
                return Ok(disaggregatedDeviceTelemetry);
            }
            else
            {
                // Create a copy of the Telemetries array
                var trimmedTelemetries = limit.HasValue ? deviceTelemetry.Telemetries.TakeLast(limit.Value).ToArray() : deviceTelemetry.Telemetries;
                var deviceTelemetryCopy = new DeviceTelemetry
                {
                    Device = deviceTelemetry.Device,
                    Telemetries = trimmedTelemetries
                };
                return Ok(deviceTelemetryCopy);
            }
        }
    }
}
