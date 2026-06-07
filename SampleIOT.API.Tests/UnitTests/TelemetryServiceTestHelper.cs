using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Models;
using SampleIOT.API.Services;
using System.Collections.Generic;
using System.Reflection;

namespace SampleIOT.API.Tests.UnitTests;

public class TelemetryServiceTestHelper : TelemetryService
{
    // Test output is SampleIOT.API.Tests\bin\..., CSV data is in SampleIOT.API\bin\...
    public static string ContentRootPath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SampleIOT.API");

    public TelemetryServiceTestHelper(IWebHostEnvironment env, ILogger<TelemetryService> logger, IDeviceService ds)
        : base(env, logger, ds) { }

    public Task Start() => StartAsync(CancellationToken.None);
    public void Dispose() => StopAsync(CancellationToken.None).GetAwaiter().GetResult();

    public bool IsInitialized => (bool)GetType().BaseType!
        .GetField("_isInitialized", BindingFlags.NonPublic | BindingFlags.Instance)!
        .GetValue(this)!;

    public int DeviceDictionaryCount => ((Dictionary<string, DeviceTelemetry>)GetType().BaseType!
        .GetField("dictionary", BindingFlags.NonPublic | BindingFlags.Instance)!
        .GetValue(this)!)
        .Count;

    public void InvokeTrim(DeviceTelemetry deviceTelemetry)
    {
        GetType().BaseType!
            .GetMethod("TryTrimDeviceTelemetry", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(this, new object[] { deviceTelemetry });
    }

    public bool TimerIsDisposed
    {
        get
        {
            var timerField = GetType().BaseType!
                .GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(this);
            if (timerField is Timer timer)
            {
                try
                {
                    timer.Change(0, 0);
                    return false;
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
            }
            return true;
        }
    }
}
