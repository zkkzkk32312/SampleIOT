using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Models;
using SampleIOT.API.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleIOT.API.Services
{
    public class TelemetryService : ITelemetryService, IDisposable
    {
        private class TelemetrySimulationFile
        {
            public Device Device { get; set; }
            public List<TelemetrySimulationFileRow> Rows { get; set; }
        }

        private class TelemetrySimulationFileRow
        {
            public DateTimeOffset TimeStamp { get; set; }
            public List<Telemetry> Telemetries { get; set; }
        }

        public Action<string, Telemetry> NewTelemetryReceived { get; set; }
        private readonly IDeviceService deviceService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<TelemetryService> _logger;
        private readonly string _telemetryDataFolderPath;
        private readonly Dictionary<string, TelemetrySimulationFile> fileDictionary = new Dictionary<string, TelemetrySimulationFile>();
        private readonly Dictionary<string, DeviceTelemetry> dictionary = new Dictionary<string, DeviceTelemetry>();
        private readonly object _simulationLock = new object();
        private Timer _timer;
        private const int TelemetryCountSoftLimit = 10000;
        private bool _isInitialized = false;

        public TelemetryService(IWebHostEnvironment webHostEnvironment, ILogger<TelemetryService> logger, IDeviceService service)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _telemetryDataFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "Telemetry");
            deviceService = service;
        }

        void Initialize ()
        {
            DirectoryInfo info = new DirectoryInfo(_telemetryDataFolderPath);
            foreach (FileInfo file in info.GetFiles("*.csv"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file.Name);
                string deviceId = GetDeviceIdFromFileName(fileName);
                TelemetrySimulationFile simulationFile = new TelemetrySimulationFile();
                DeviceTelemetry deviceTelemetry2 = new DeviceTelemetry();
                simulationFile.Device = deviceService.GetDevice(deviceId);
                deviceTelemetry2.Device = deviceService.GetDevice(deviceId);

                simulationFile.Rows = new List<TelemetrySimulationFileRow>();
                List<Telemetry> telemetries2 = new List<Telemetry>();

                using (StreamReader reader = new StreamReader(file.FullName))
                {
                    string[] telemetryNames = new string[0];
                    string headerLine = reader.ReadLine();
                    if (headerLine != null)
                    {
                        telemetryNames = headerLine.Split(',');
                    }

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] fields = line.Split(',');
                        DateTimeOffset timeOfDay = DateTimeOffset.Parse(fields[0]);
                        DateTimeOffset now = DateTimeOffset.Now;

                        var row = new TelemetrySimulationFileRow();
                        row.TimeStamp = timeOfDay;
                        row.Telemetries = new List<Telemetry>();

                        for (int i = 1; i < fields.Length; i++)
                        {
                            row.Telemetries.Add(new Telemetry { Key = telemetryNames[i], Value = fields[i], TimeStamp = timeOfDay });

                            if (timeOfDay <= now)
                            {
                                telemetries2.Add(new Telemetry { Key = telemetryNames[i], Value = fields[i], TimeStamp = timeOfDay });
                            }
                        }
                        simulationFile.Rows.Add(row);
                    }
                    deviceTelemetry2.Telemetries = telemetries2.ToArray();
                }
                _logger.LogInformation(simulationFile.Device + " " + simulationFile.Rows.Count());
                fileDictionary.TryAdd(deviceId, simulationFile);
                dictionary.TryAdd(deviceId, deviceTelemetry2);
            }
            StartSimulation();
        }

        public DeviceTelemetry GetTelemetry(string deviceId)
        {
            if (!_isInitialized)
            {
                _logger.LogWarning("TelemetryService not initialized yet, returning null for device: " + deviceId);
                return null;
            }

            lock (_simulationLock)
            {
                return dictionary.TryGetValue(deviceId, out var deviceTelemetry) ? deviceTelemetry : null;
            }
        }

        string GetDeviceIdFromFileName (string fileName)
        {
            int indexOfSeparator = fileName.IndexOf('_');
            return fileName.Substring(indexOfSeparator + 1);
        }

        public async Task Start()
        {
            if (_isInitialized) return;

            _logger.LogInformation("Starting telemetry service initialization...");
            await Task.Run(() => Initialize());
            _isInitialized = true;
            _logger.LogInformation("Telemetry service initialization completed.");
        }

        void StartSimulation ()
        {
            _timer = new Timer(Simulate, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        void Simulate (object state)
        {
            lock (_simulationLock)
            {
                DateTimeOffset now = DateTimeOffset.Now;

                foreach (var kvp in dictionary)
                {
                    var deviceId = kvp.Key;

                    if (!fileDictionary.TryGetValue(deviceId, out var fileDeviceTelemetry))
                        continue;

                    var simulationRow = fileDeviceTelemetry.Rows.FirstOrDefault(x => x.TimeStamp.TimeOfDay > now.TimeOfDay);

                    if (simulationRow == null)
                    {
                        _logger.LogInformation("Simulation reached the end of daily cycle, current time :" + now.ToString("HH:mm:ss"));
                        continue;
                    }

                    if (!dictionary.TryGetValue(deviceId, out var deviceTelemetry))
                        continue;

                    var updatedTelemetryList = new List<Telemetry>(deviceTelemetry.Telemetries);

                    if (updatedTelemetryList.Count == 0)
                        continue;

                    foreach (var telemetry in simulationRow.Telemetries)
                    {
                        updatedTelemetryList.Add(telemetry);
                        NewTelemetryReceived?.Invoke(deviceId, telemetry);
                    }
                    deviceTelemetry.Telemetries = updatedTelemetryList.ToArray();

                    TryTrimDeviceTelemetry(deviceTelemetry);
                }
            }
        }

        void StopSimulation ()
        {
            _timer?.Change(Timeout.Infinite, 0);
        }

        void TryTrimDeviceTelemetry (DeviceTelemetry deviceTelemetry)
        {
            if (deviceTelemetry != null &&
                deviceTelemetry.Telemetries != null &&
                deviceTelemetry.Telemetries.Length > TelemetryCountSoftLimit)
            {
                int currentLength = deviceTelemetry.Telemetries.Length;
                Telemetry[] trimmedArray = new Telemetry[TelemetryCountSoftLimit/2];
                Array.Copy(deviceTelemetry.Telemetries, currentLength - TelemetryCountSoftLimit/2, trimmedArray, 0, TelemetryCountSoftLimit/2);
                deviceTelemetry.Telemetries = trimmedArray;
                _logger.LogInformation($"Telemetry array trimmed for {deviceTelemetry.Device.Id.ToString()}");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
