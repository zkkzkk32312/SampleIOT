using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Models;
using SampleIOT.API.Services;
using SampleIOT.API.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public IActionResult Get([FromQuery] string sort)
        {
            var devices = deviceService.GetDevices();

            // Apply sorting based on the 'sort' query parameter
            if (!string.IsNullOrEmpty(sort))
            {
                if (sort == "id")
                {
                    devices = devices.OrderBy(device => device.Id);
                }
                else if (sort == "type")
                {
                    devices = devices.OrderBy(device => device.Type);
                }
                // Add more sorting criteria as needed
            }

            // Check the Accept header to determine the desired response format
            var acceptHeader = Request.Headers["Accept"].ToString();

            if (acceptHeader.Contains("application"))
            {
                // Return JSON data
                if (devices != null)
                    return Ok(devices);
                else
                {
                    _logger.LogWarning("DeviceController.Get() returns null");
                    return NotFound();
                }
            }
            else
            {
                // Generate HTML content for HTMX
                string htmlContent = GetDevicesHtml(devices);
                return Content(htmlContent, "text/html");
            }
        }


        [HttpGet("{id}")]
        public IActionResult GetDevice(string id)
        {
            var device = deviceService.GetDevice(id);
            if (device == null)
            {
                return NotFound(); // Return 404 Not Found if device is not found
            }

            // Check the Accept header to determine the desired response format
            var acceptHeader = Request.Headers["Accept"].ToString();
            if (acceptHeader.Contains("text/html"))
            {
                // Generate HTML content for HTMX
                string htmlContent = GetDevicesHtml(new List<Device> { device });
                return Content(htmlContent, "text/html");
            }
            else
            {
                // Return JSON data
                return Ok(device);
            }
        }

        private string GetDevicesHtml (IEnumerable<Device> list)
        {
            string html = string.Empty;
            foreach (var device in list)
            {
                //htmlContent += "<div class=\"flex items-center py-4\">";
                //htmlContent += $"<div class=\"flex-grow\">{device.Id}</div>";
                //htmlContent += $"<div class=\"\">{device.Type}</div>";
                //htmlContent += "</div>";

                html += "<tr>";
                html += $"<td class=\"whitespace-nowrap px-4 py-2\">{device.Id}</div>";
                html += $"<td class=\"whitespace-nowrap px-4 py-2\">{device.Type}</div>";
                html += "<tr>";
            }
            return html;
        }
    }
}
