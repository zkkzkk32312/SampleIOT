using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Models;
using SampleIOT.API.Services;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SampleIOT.API.Controllers
{
    [Route("api/[controller]")]
    [Route("Telemetry")]
    [ApiController]
    public class TelemetrySSEController : ControllerBase
    {
        private ITelemetryService telemetryService;
        private readonly ILogger<TelemetrySSEController> _logger;
        private readonly Guid _clientId;
        private string _targetDevice;
        private Action<string, Telemetry> _handler;

        public TelemetrySSEController(ITelemetryService service, ILogger<TelemetrySSEController> logger)
        {
            this.telemetryService = service;
            this._logger = logger;
            _clientId = Guid.NewGuid();
            _targetDevice = string.Empty;
        }

        private async Task OnNewTelemetryReceived(string deviceId, Telemetry telemetry)
        {
            try
            {
                var json = JsonSerializer.Serialize(telemetry);

                if (_targetDevice != null && _targetDevice.CompareTo(deviceId) == 0)
                {
                    if (Response.Body != null && Response.Body.CanWrite)
                    {
                        var currentTime = DateTimeOffset.Now.ToString("HH:mm:ss");
                        var message = $"event: Telemetry\ndata: <div>Content to swap into your HTML page. Client ID: {_clientId}. Current Time: {currentTime}. Telemetry: {json}</div>\n\n";
                        _logger.LogInformation($"*****OnNewTelemetryReceived***** : Client: {_clientId}, Device ID : {deviceId}, Telemetry : {json}");
                        await SendMessage(message);
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogError("**********ObjectDisposedException********");
                _logger.LogError(ex, "Response has been disposed before the message could be sent.");
                telemetryService.NewTelemetryReceived -= _handler;
            }
            catch (Exception ex)
            {
                _logger.LogError("**********Exception********");
                _logger.LogError(ex, "An error occurred while processing telemetry data.");
            }
        }

        // SSE endpoint to subscribe to telemetry updates
        [HttpGet("Subscribe/{deviceId}")]
        public async Task Subscribe(string deviceId, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");

            _targetDevice = deviceId;
            _handler = async (d, t) => await OnNewTelemetryReceived(d, t);

            _logger.LogInformation($"*****SUSCRIBE*****: {_clientId} subscribed for device {deviceId}");
            telemetryService.NewTelemetryReceived += _handler;

            var currentTime = DateTimeOffset.Now.ToString("HH:mm:ss");
            await SendMessage($"event: Telemetry\ndata: <div>Content to swap into your HTML page. Client ID: {_clientId}. Current Time: {currentTime}.</div>\n\n");

            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(() =>
            {
                _logger.LogInformation($"*****DISCONNECT*****: {_clientId} had disconnected");
                tcs.SetResult(true);
            });
            await tcs.Task;
            CleanUp();
        }

        // Helper method to send SSE messages to clients
        private async Task SendMessage(string message)
        {
            await Response.WriteAsync(message);
            await Response.Body.FlushAsync();
        }

        private void CleanUp()
        {
            telemetryService.NewTelemetryReceived -= _handler;
            Response.Body.Close();
        }
    }
}
