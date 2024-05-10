using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Services.Interface;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SampleIOT.API.Controllers
{
    [Route("api/[controller]")]
    [Route("Telemetry")]
    [ApiController]
    public class TelemetryController : ControllerBase
    {
        private ITelemetryService telemetryService;
        private readonly ILogger<DeviceController> _logger;

        public TelemetryController(ITelemetryService service, ILogger<DeviceController> logger)
        {
            this.telemetryService = service;
            this._logger = logger;
        }

        // GET api/<TelemetryController>/5
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            var deviceTelemetry = telemetryService.GetTelemetry(id);

            // Return JSON data
            if (deviceTelemetry != null)
                return Ok(deviceTelemetry);
            else
            {
                _logger.LogWarning("DeviceController.Get() returns null");
                return NotFound();
            }
        }
    }
}
