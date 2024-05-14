using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using SampleIOT.API.Models;
using SampleIOT.API.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SampleIOT.API.Services
{
    public class TelemetryService : ITelemetryService, IDisposable
    {
        public Action<string, Telemetry> NewTelemetryReceived { get; set; }
        private readonly IDeviceService deviceService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<TelemetryService> _logger;
        private readonly string _telemetryDataFolderPath;
        private Dictionary<string, DeviceTelemetry> fileDictionary = new Dictionary<string, DeviceTelemetry>();
        private Dictionary<string, DeviceTelemetry> dictionary = new Dictionary<string, DeviceTelemetry>();
        private Timer _timer;

        public TelemetryService(IWebHostEnvironment webHostEnvironment, ILogger<TelemetryService> logger, IDeviceService service)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _telemetryDataFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "Telemetry");
            deviceService = service;
            Initialize();
        }

        void Initialize ()
        {
            DirectoryInfo info = new DirectoryInfo(_telemetryDataFolderPath);
            foreach (FileInfo file in info.GetFiles("*.csv"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file.Name);
                string deviceId = GetDeviceIdFromFileName(fileName);
                DeviceTelemetry deviceTelemetry = new DeviceTelemetry();
                DeviceTelemetry deviceTelemetry2 = new DeviceTelemetry();
                deviceTelemetry.Device = deviceService.GetDevice(deviceId);
                deviceTelemetry2.Device = deviceService.GetDevice(deviceId);

                List<Telemetry> telemetries = new List<Telemetry>();
                List<Telemetry> telemetries2 = new List<Telemetry>();

                using (TextFieldParser parser = new TextFieldParser(file.FullName))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    string[] telemetryNames = new string[0];
                    //Telemetry Names
                    if (!parser.EndOfData)
                    {
                        telemetryNames = parser.ReadFields();
                    }

                    //Telemetry Data
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        DateTimeOffset timeOfDay = DateTimeOffset.Parse(fields[0]);
                        DateTimeOffset now = DateTimeOffset.Now;

                        for (int i = 1; i < fields.Length; i++)
                        {
                            telemetries.Add(new Telemetry { Key = telemetryNames[i], Value = fields[i], TimeStamp = timeOfDay });
                            if (timeOfDay <= now)
                            {
                                telemetries2.Add(new Telemetry { Key = telemetryNames[i], Value = fields[i], TimeStamp = timeOfDay });
                            }
                        }
                    }
                    deviceTelemetry.Telemetries = telemetries.ToArray();
                    deviceTelemetry2.Telemetries = telemetries2.ToArray();
                }
                _logger.LogInformation(deviceTelemetry.Device + " " + deviceTelemetry.Telemetries.Count());
                fileDictionary.Add(deviceId, deviceTelemetry);
                dictionary.Add(deviceId, deviceTelemetry2);

                StartSimulation();
            }
        }

        public DeviceTelemetry GetTelemetry(string deviceId)
        {
            if (dictionary.ContainsKey(deviceId))
            {
                return dictionary[deviceId];
            }
            return null;
        }

        string GetDeviceIdFromFileName (string fileName)
        {
            int indexOfSeparator = fileName.IndexOf('_');
            return fileName.Substring(indexOfSeparator + 1);
        }

        public void Start()
        {
        }

        void StartSimulation ()
        {
            _timer = new Timer(Simulate, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
        }

        void Simulate (object state)
        {
            DateTimeOffset now = DateTimeOffset.Now;

            foreach (var kvp in dictionary)
            {
                var deviceId = kvp.Key;
                var fileDeviceTelemetry = fileDictionary[deviceId];
                var fileTelemetryArray = fileDeviceTelemetry.Telemetries;
                var simulatedTelemetry = fileTelemetryArray.FirstOrDefault(x => x.TimeStamp > now);

                if (simulatedTelemetry == null)
                {
                    _logger.LogInformation("Fucked up");
                }
                else
                {
                    var updatedTelemetryList = kvp.Value.Telemetries.ToList();
                    updatedTelemetryList.Add(simulatedTelemetry);
                    kvp.Value.Telemetries = updatedTelemetryList.ToArray();
                    _logger.LogInformation("Simulated Telemetry entries added for " + deviceId);
                    NewTelemetryReceived?.Invoke(deviceId, simulatedTelemetry);
                }
            }
        }

        void StopSimulation ()
        {
            _timer?.Change(Timeout.Infinite, 0);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
