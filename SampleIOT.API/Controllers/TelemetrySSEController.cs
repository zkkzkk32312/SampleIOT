﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SampleIOT.API.Services;
using static SampleIOT.API.Controllers.TelemetryController;
using System.Threading.Tasks;
using System.Threading;
using System;
using SampleIOT.API.Models;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Services.Interface;

namespace SampleIOT.API.Controllers
{
    [Route("api/[controller]")]
    [Route("Telemetry")]
    [ApiController]
    public class TelemetrySSEController : ControllerBase
    {
        private ITelemetryService telemetryService;
        private readonly ILogger<TelemetrySSEController> _logger;
        private Guid clientId;
        private string targetDevice;

        public TelemetrySSEController(ITelemetryService service, ILogger<TelemetrySSEController> logger)
        {
            this.telemetryService = service;
            this._logger = logger;
            clientId = Guid.NewGuid();
            targetDevice = string.Empty;
        }

        private async void OnNewTelemetryReceived(string deviceId, Telemetry telemetry)
        {
            var clients = new List<Guid>();
            var json = JsonSerializer.Serialize(telemetry);

            if (targetDevice != null && targetDevice.CompareTo(deviceId) == 0)
            {
                if (Response.Body != null && Response.Body.CanWrite)
                {
                    var currentTime = DateTimeOffset.Now.ToString("HH:mm:ss");
                    var message = $"event: Telemetry\ndata: <div>Content to swap into your HTML page. Client ID: {clientId}. Current Time: {currentTime}. Telemetry: {json}</div>\n\n";
                    _logger.LogInformation($"*****OnNewTelemetryReceived***** : Client: {clientId}, Device ID : {deviceId}, Telemetry : {json}");
                    await SendMessage(message);
                }
            }
        }

        // SSE endpoint to subscribe to telemetry updates
        [HttpGet("Subscribe/{deviceId}")]
        public async Task<IActionResult> Subscribe(string deviceId, CancellationToken cancellationToken)
        {

            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Access-Control-Allow-Origin", "*");

            targetDevice = deviceId;

            _logger.LogInformation($"*****SUSCRIBE*****: {clientId} subscribed for device {deviceId}");
            telemetryService.NewTelemetryReceived += OnNewTelemetryReceived;

            var currentTime = DateTimeOffset.Now.ToString("HH:mm:ss");
            await SendMessage($"event: Telemetry\ndata: <div>Content to swap into your HTML page. Client ID: {clientId}. Current Time: {currentTime}.</div>\n\n");

            // Use TaskCompletionSource to create a Task that completes when the cancellation token is triggered
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(() =>
            {
                tcs.SetResult(true);
            });
            await Task.Run(()=> tcs.Task);
            CleanUp();
            return Ok();
        }

        // SSE endpoint to unsubscribe from telemetry updates
        [HttpGet("Unsubscribe")]
        public IActionResult Unsubscribe()
        {
            CleanUp();
            return Ok();
        }

        // Helper method to send SSE messages to clients
        private async Task SendMessage(string message)
        {
            await Response.WriteAsync(message);
            await Response.Body.FlushAsync();
        }

        private void CleanUp()
        {
            telemetryService.NewTelemetryReceived -= OnNewTelemetryReceived;
            Response.Body.Close();
        }
    }
}
