﻿using Microsoft.AspNetCore.Hosting;
using SampleIOT.API.Models;
using SampleIOT.API.Services.Interface;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SampleIOT.API.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private string _jsonPathName;
        private readonly IEnumerable<Device> _devices;

        public DeviceService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _jsonPathName = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "Device", "Devices.json");
            _devices = LoadDevicesFromJsonFile();
        }

        private IEnumerable<Device> LoadDevicesFromJsonFile()
        {
            // Read the JSON file and deserialize it into a list of devices
            string json = File.ReadAllText(_jsonPathName);
            return JsonSerializer.Deserialize<IEnumerable<Device>>(json);
        }

        public IEnumerable<Device> GetDevices()
        {
            return _devices;
        }

        public Device GetDevice(string id)
        {
            return _devices.FirstOrDefault(x => x.Id == id);
        }

        public Task Start ()
        {
            return Task.CompletedTask;
        }
    }
}
