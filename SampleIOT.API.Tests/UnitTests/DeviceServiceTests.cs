using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SampleIOT.API.Models;
using SampleIOT.API.Services;

namespace SampleIOT.API.Tests.UnitTests;

public class DeviceServiceTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_DoesNotAutoInitialize()
    {
        var service = CreateService();
        Assert.False(service.IsInitialized);
    }

    // --- GetDevices ---

    [Fact]
    public void GetDevices_NotInitialized_ReturnsEmpty()
    {
        var service = CreateService();
        var result = service.GetDevices();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDevices_AfterInit_ReturnsAllDevices()
    {
        var service = CreateService();
        await service.Start();

        try
        {
            var devices = service.GetDevices();
            Assert.NotEmpty(devices);
        }
        finally
        {
            service.Dispose();
        }
    }

    // --- GetDevice ---

    [Fact]
    public void GetDevice_NotInitialized_ReturnsNull()
    {
        var service = CreateService();
        var result = service.GetDevice("680539");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDevice_ExistingId_ReturnsDevice()
    {
        var service = CreateService();
        await service.Start();

        try
        {
            var result = service.GetDevice("680539");
            Assert.NotNull(result);
            Assert.Equal("680539", result.Id);
            Assert.Equal("Appliance", result.Type);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task GetDevice_NonExistentId_ReturnsNull()
    {
        var service = CreateService();
        await service.Start();

        try
        {
            var result = service.GetDevice("999999");
            Assert.Null(result);
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

    // --- Stop ---

    [Fact]
    public async Task Stop_CompletesWithoutError()
    {
        var service = CreateService();
        await service.Start();
        service.Dispose();
    }

    // --- Device data validation ---

    [Fact]
    public async Task GetDevices_ContainsExpectedDeviceTypes()
    {
        var service = CreateService();
        await service.Start();

        try
        {
            var devices = service.GetDevices().ToList();
            var types = devices.Select(d => d.Type).Distinct().ToList();
            Assert.Contains("Appliance", types);
            Assert.Contains("LightFixture", types);
            Assert.Contains("SolarPanel", types);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task GetDevice_SolarPanel_HasTelemetryNames()
    {
        var service = CreateService();
        await service.Start();

        try
        {
            var result = service.GetDevice("800280");
            Assert.NotNull(result);
            Assert.Equal("SolarPanel", result.Type);
            Assert.Equal(new[] { "Power_Generated", "Temperature", "Voltage" }, result.TelemetryNames);
        }
        finally
        {
            service.Dispose();
        }
    }

    static DeviceServiceTestHelper CreateService()
    {
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(x => x.ContentRootPath).Returns(DeviceServiceTestHelper.ContentRootPath);
        var mockLogger = new Mock<ILogger<DeviceService>>();
        return new DeviceServiceTestHelper(mockEnv.Object, mockLogger.Object);
    }
}
