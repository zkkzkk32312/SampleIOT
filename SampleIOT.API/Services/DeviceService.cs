using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SampleIOT.API.Services
{
    public class DeviceService : IDeviceService, IHostedService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<DeviceService> _logger;
        private string _jsonPathName;
        private IEnumerable<Device> _devices;
        private bool _isInitialized = false;

        public DeviceService(IWebHostEnvironment webHostEnvironment, ILogger<DeviceService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _jsonPathName = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "Device", "Devices.json");
        }

        private IEnumerable<Device> LoadDevicesFromJsonFile()
        {
            string json = File.ReadAllText(_jsonPathName);
            return JsonSerializer.Deserialize<IEnumerable<Device>>(json);
        }

        public IEnumerable<Device> GetDevices()
        {
            if (!_isInitialized)
            {
                _logger.LogWarning("DeviceService not initialized yet, returning empty device list");
                return Enumerable.Empty<Device>();
            }
            return _devices;
        }

        public Device GetDevice(string id)
        {
            if (!_isInitialized)
            {
                _logger.LogWarning("DeviceService not initialized yet, returning null for device: " + id);
                return null;
            }
            return _devices.FirstOrDefault(x => x.Id == id);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized) return Task.CompletedTask;

            _logger.LogInformation("Starting device service initialization...");
            _devices = LoadDevicesFromJsonFile();
            _isInitialized = true;
            _logger.LogInformation("Device service initialization completed. Loaded " + _devices.Count() + " devices.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
