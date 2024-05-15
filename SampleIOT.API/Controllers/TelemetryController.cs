using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Models;
using SampleIOT.API.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SampleIOT.API.Controllers
{
    [Route("api/[controller]")]
    [Route("Telemetry")]
    [ApiController]
    public class TelemetryController : ControllerBase
    {
        public class Subscription
        {
            public string deviceId { get; set; }
            public CancellationToken CancellationToken { get; set; }
            public HttpResponse response { get; set; }
        }

        private ITelemetryService telemetryService;
        private readonly ILogger<TelemetryController> _logger;
        // Dictionary to store SSE clients
        private static readonly Dictionary<Guid, Subscription> Clients = new Dictionary<Guid, Subscription>();
        private static readonly Dictionary<string, HashSet<Guid>> DeviceTelemetrySubscribers = new Dictionary<string, HashSet<Guid>>();

        public TelemetryController(ITelemetryService service, ILogger<TelemetryController> logger)
        {
            this.telemetryService = service;
            this._logger = logger;
            service.NewTelemetryReceived += OnNewTelemetryReceived;
        }

        private void OnNewTelemetryReceived(string deviceId, Telemetry telemetry)
        {
            _logger.LogInformation("*****NEWTELEMETRY*****" + deviceId + "*****NEWTELEMETRY******");

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
                        Telemetries = limit.HasValue ? y.Take(limit.Value).ToArray() : y.ToArray()
                    })
                    .ToList();
                return Ok(disaggregatedDeviceTelemetry);
            }
            else
            {
                // Create a copy of the Telemetries array
                var trimmedTelemetries = limit.HasValue ? deviceTelemetry.Telemetries.Take(limit.Value).ToArray() : deviceTelemetry.Telemetries;
                var deviceTelemetryCopy = new DeviceTelemetry
                {
                    Device = deviceTelemetry.Device,
                    Telemetries = trimmedTelemetries
                };
                return Ok(deviceTelemetryCopy);
            }
        }

        // SSE endpoint to subscribe to telemetry updates
        [HttpGet("Subscribe/{id}")]
        public async Task<IActionResult> Subscribe(string id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("*****SUSCRIBE*****" + id + "*****SUSCRIBE******");

            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var clientId = Guid.NewGuid();
            var subscription = new Subscription();
            subscription.CancellationToken = cancellationToken;
            subscription.response = Response;
            subscription.deviceId = id;

            Clients.Add(clientId, subscription);

            while (true)
            {
                if (!Response.Body.CanWrite) // Check if the connection is still open
                {
                    Clients.Remove(clientId); // Remove the client from the dictionary
                    return Ok(); // Return HTTP 200 OK to indicate successful unsubscribe
                }

                // Simulate sending telemetry data for the specified ID every 5 seconds
                var currentTime = DateTimeOffset.Now.ToString("HH:mm:ss");
                var message = $"event: Telemetry\ndata: <div>Content to swap into your HTML page. Client ID: {clientId}. Current Time: {currentTime}</div>\n\n";
                await SendMessage(clientId, message);

                Thread.Sleep(5000);
            }
        }

        // SSE endpoint to unsubscribe from telemetry updates
        [HttpGet("Unsubscribe/{clientId}")]
        public IActionResult Unsubscribe(Guid clientId)
        {
            if (Clients.ContainsKey(clientId))
            {
                // Close the response stream to force client disconnection
                Clients[clientId].response.Body.Close();
                Clients.Remove(clientId);
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        // Helper method to send SSE messages to clients
        private async Task SendMessage(Guid clientId, string message)
        {
            if (Clients.ContainsKey(clientId))
            {
                await Clients[clientId].response.WriteAsync(message);
                await Clients[clientId].response.Body.FlushAsync();
            }
        }
    }
}
