using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Models;
using SampleIOT.API.Services;
using System.Collections.Generic;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SampleIOT.API.Controllers
{
    [Route("[controller]")]
    [Route("Devices")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private DeviceService deviceService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(DeviceService service, ILogger<DeviceController> logger) 
        {
            this.deviceService = service;
            this._logger = logger;
        }

        // GET: api/<DeviceController>
        [HttpGet]
        public IActionResult Get()
        {
            var devices = deviceService.GetDevices();

            if (devices != null)
                return Ok(devices);
            else
            {
                _logger.LogWarning("DeviceController.Get() returns null");
                return NotFound();
            }
        }
    }
}
