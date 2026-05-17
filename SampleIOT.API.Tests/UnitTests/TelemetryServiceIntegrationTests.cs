using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SampleIOT.API.Models;
using SampleIOT.API.Services;
using SampleIOT.API.Services.Interface;

namespace SampleIOT.API.Tests.UnitTests;

public class TelemetryServiceIntegrationTests
{
    [Fact]
    public async Task NewTelemetryReceived_EventFiresDuringSimulation()
    {
        var mockDeviceService = new Mock<IDeviceService>();
        mockDeviceService.Setup(s => s.GetDevice(It.IsAny<string>()))
            .Returns(new Device { Id = "680539", Type = "Cooktop" });

        var service = CreateService(mockDeviceService.Object);
        var tcs = new TaskCompletionSource<(string DeviceId, Telemetry Telemetry)>();

        service.NewTelemetryReceived += (deviceId, telemetry) =>
        {
            tcs.TrySetResult((deviceId, telemetry));
        };

        await service.Start();

        try
        {
            var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotNull(result.DeviceId);
            Assert.NotNull(result.Telemetry);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task Dispose_StopsSimulationTimer()
    {
        var mockDeviceService = new Mock<IDeviceService>();
        mockDeviceService.Setup(s => s.GetDevice(It.IsAny<string>()))
            .Returns(new Device { Id = "680539", Type = "Cooktop" });

        var service = CreateService(mockDeviceService.Object);
        int callbackCount = 0;

        service.NewTelemetryReceived += (deviceId, telemetry) =>
        {
            Interlocked.Increment(ref callbackCount);
        };

        await service.Start();
        await Task.Delay(6000);

        int countBeforeDispose = callbackCount;
        service.Dispose();

        await Task.Delay(6000);

        Assert.Equal(countBeforeDispose, callbackCount);
    }

    static TelemetryServiceTestHelper CreateService(IDeviceService? deviceService = null)
    {
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(x => x.ContentRootPath).Returns(TelemetryServiceTestHelper.ContentRootPath);
        var mockLogger = new Mock<ILogger<TelemetryService>>();
        var mockDeviceService = deviceService ?? new Mock<IDeviceService>().Object;
        return new TelemetryServiceTestHelper(mockEnv.Object, mockLogger.Object, mockDeviceService);
    }
}
