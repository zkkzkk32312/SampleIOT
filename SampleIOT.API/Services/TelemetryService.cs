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
        private Dictionary<string, TelemetrySimulationFile> fileDictionary = new Dictionary<string, TelemetrySimulationFile>();
        private Dictionary<string, DeviceTelemetry> dictionary = new Dictionary<string, DeviceTelemetry>();
        private Timer _timer;
        private const int TelemetryCountSoftLimit = 10000;

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
                TelemetrySimulationFile simulationFile = new TelemetrySimulationFile();
                DeviceTelemetry deviceTelemetry2 = new DeviceTelemetry();
                simulationFile.Device = deviceService.GetDevice(deviceId);
                deviceTelemetry2.Device = deviceService.GetDevice(deviceId);


                //List<Telemetry> telemetries = new List<Telemetry>();
                simulationFile.Rows = new List<TelemetrySimulationFileRow>();
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
                    //telemetryFile.Telemetries = telemetries.ToArray();
                    deviceTelemetry2.Telemetries = telemetries2.ToArray();
                }
                _logger.LogInformation(simulationFile.Device + " " + simulationFile.Rows.Count());
                fileDictionary.Add(deviceId, simulationFile);
                dictionary.Add(deviceId, deviceTelemetry2);
            }
            StartSimulation();
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
            _timer = new Timer(Simulate, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        void Simulate (object state)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            //_logger.LogInformation("******SIMULATE****** : " + now.ToString());

            foreach (var kvp in dictionary)
            {
                var deviceId = kvp.Key;
                var fileDeviceTelemetry = fileDictionary[deviceId];
                var simulationRow = fileDeviceTelemetry.Rows.FirstOrDefault(x => x.TimeStamp.TimeOfDay > now.TimeOfDay);

                if (simulationRow == null)
                {
                    _logger.LogInformation("Simulation reached the end of daily cycle, current time :" + now.ToString("HH:mm:ss"));
                    continue;
                }

                var updatedTelemetryList = new List<Telemetry>(kvp.Value.Telemetries);

                if (updatedTelemetryList.Count == 0)
                    continue;

                foreach(var telemetry in simulationRow.Telemetries)
                {
                    updatedTelemetryList.Add(telemetry);
                    NewTelemetryReceived?.Invoke(deviceId, telemetry);
                }
                kvp.Value.Telemetries = updatedTelemetryList.ToArray();

                TryTrimDeviceTelemetry(kvp.Value);
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
