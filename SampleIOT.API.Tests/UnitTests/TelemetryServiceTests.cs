using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SampleIOT.API.Models;
using SampleIOT.API.Services;
using SampleIOT.API.Services.Interface;

namespace SampleIOT.API.Tests.UnitTests;

public class TelemetryServiceTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_DoesNotAutoInitialize()
    {
        var service = CreateService();
        Assert.False(service.IsInitialized);
    }

    // --- GetTelemetry ---

    [Fact]
    public void GetTelemetry_NotInitialized_ReturnsNull()
    {
        var service = CreateService();
        var result = service.GetTelemetry("680539");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTelemetry_NonExistentDevice_ReturnsNull()
    {
        var service = CreateService();
        await service.Start();

        try
        {
            var result = service.GetTelemetry("999999");
            Assert.Null(result);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task GetTelemetry_ExistingDevice_ReturnsData()
    {
        var mockDeviceService = new Mock<IDeviceService>();
        mockDeviceService.Setup(s => s.GetDevice(It.IsAny<string>()))
            .Returns(new Device { Id = "680539", Type = "Cooktop" });

        var service = CreateService(mockDeviceService.Object);
        await service.Start();

        try
        {
            var result = service.GetTelemetry("680539");

            Assert.NotNull(result);
            Assert.Equal("680539", result.Device.Id);
            Assert.NotEmpty(result.Telemetries);
        }
        finally
        {
            service.Dispose();
        }
    }

    // --- Start ---

    [Fact]
    public async Task Start_TransitionsFromNotInitializedToInitialized()
    {
        var service = CreateService();
        Assert.False(service.IsInitialized);

        try
        {
            await service.Start();
            Assert.True(service.IsInitialized);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task Start_AlreadyInitialized_IsNoOp()
    {
        var service = CreateService();

        try
        {
            await service.Start();
            Assert.True(service.IsInitialized);

            await service.Start();
            Assert.True(service.IsInitialized);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task Start_PopulatesDeviceDictionary()
    {
        var mockDeviceService = new Mock<IDeviceService>();
        mockDeviceService.Setup(s => s.GetDevice(It.IsAny<string>()))
            .Returns(new Device { Id = "680539", Type = "Cooktop" });

        var service = CreateService(mockDeviceService.Object);

        try
        {
            await service.Start();
            Assert.True(service.DeviceDictionaryCount > 0);
        }
        finally
        {
            service.Dispose();
        }
    }

    // --- TryTrimDeviceTelemetry ---

    [Fact]
    public void TryTrimDeviceTelemetry_AboveLimit_TrimsToHalfLimit()
    {
        var service = CreateService();
        var deviceTelemetry = new DeviceTelemetry
        {
            Device = new Device { Id = "1" },
            Telemetries = new Telemetry[15000]
        };

        service.InvokeTrim(deviceTelemetry);

        Assert.Equal(5000, deviceTelemetry.Telemetries.Length);
    }

    [Fact]
    public void TryTrimDeviceTelemetry_BelowLimit_NoChange()
    {
        var service = CreateService();
        var deviceTelemetry = new DeviceTelemetry
        {
            Device = new Device { Id = "1" },
            Telemetries = new Telemetry[8000]
        };

        service.InvokeTrim(deviceTelemetry);

        Assert.Equal(8000, deviceTelemetry.Telemetries.Length);
    }

    [Fact]
    public void TryTrimDeviceTelemetry_NullDeviceTelemetry_NoException()
    {
        var service = CreateService();
        service.InvokeTrim(null!);
    }

    [Fact]
    public void TryTrimDeviceTelemetry_NullTelemetries_NoException()
    {
        var service = CreateService();
        var deviceTelemetry = new DeviceTelemetry
        {
            Device = new Device { Id = "1" },
            Telemetries = null!
        };

        service.InvokeTrim(deviceTelemetry);
    }

    [Fact]
    public void TryTrimDeviceTelemetry_ExactlyAtLimit_NoTrim()
    {
        var service = CreateService();
        var deviceTelemetry = new DeviceTelemetry
        {
            Device = new Device { Id = "1" },
            Telemetries = new Telemetry[10000]
        };

        service.InvokeTrim(deviceTelemetry);

        Assert.Equal(10000, deviceTelemetry.Telemetries.Length);
    }

    static TelemetryServiceTestHelper CreateService(IDeviceService? deviceService = null)
    {
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(x => x.ContentRootPath).Returns(TelemetryServiceTestHelper.ContentRootPath);
        var mockLogger = new Mock<ILogger<TelemetryService>>();
        var mockDeviceService = deviceService as IDeviceService
            ?? new Mock<IDeviceService>().Object;
        return new TelemetryServiceTestHelper(mockEnv.Object, mockLogger.Object, mockDeviceService);
    }
}
