using Microsoft.AspNetCore.Hosting;
using SampleIOT.API.Models;
using SampleIOT.API.Services.Interface;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SampleIOT.API.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private string _jsonPathName;

        public DeviceService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;

            _jsonPathName = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "Data.json");
        }

        public IEnumerable<Device> GetDevices()
        {
            using (var jsonFileReader = File.OpenText(_jsonPathName))
            {
                return JsonSerializer.Deserialize<Device[]>(
                    jsonFileReader.ReadToEnd(),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
            }
        }
    }
}
