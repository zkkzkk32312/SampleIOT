using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Models;
using SampleIOT.API.Services;
using System.Collections.Generic;
using System.Reflection;

namespace SampleIOT.API.Tests.UnitTests;

public class DeviceServiceTestHelper : DeviceService
{
    public static string ContentRootPath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SampleIOT.API");

    public DeviceServiceTestHelper(IWebHostEnvironment env, ILogger<DeviceService> logger)
        : base(env, logger) { }

    public Task Start() => StartAsync(CancellationToken.None);
    public void Dispose() => StopAsync(CancellationToken.None).GetAwaiter().GetResult();

    public bool IsInitialized => (bool)GetType().BaseType!
        .GetField("_isInitialized", BindingFlags.NonPublic | BindingFlags.Instance)!
        .GetValue(this)!;

    public IEnumerable<Device> Devices => (IEnumerable<Device>)GetType().BaseType!
        .GetField("_devices", BindingFlags.NonPublic | BindingFlags.Instance)!
        .GetValue(this)!;
}
