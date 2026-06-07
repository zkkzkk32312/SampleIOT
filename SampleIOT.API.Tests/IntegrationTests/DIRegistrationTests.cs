using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SampleIOT.API.Services;

namespace SampleIOT.API.Tests.IntegrationTests;

public class DIRegistrationTests : IClassFixture<WebApplicationFactory<SampleIOT.API.Program>>
{
    private readonly WebApplicationFactory<SampleIOT.API.Program> _factory;

    public DIRegistrationTests(WebApplicationFactory<SampleIOT.API.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void InterfaceAndConcreteResolveToSameInstance()
    {
        var sp = _factory.Services;

        var deviceInterface = sp.GetRequiredService<IDeviceService>();
        var deviceConcrete = sp.GetRequiredService<DeviceService>();
        Assert.Same(deviceInterface, deviceConcrete);

        var telemetryInterface = sp.GetRequiredService<ITelemetryService>();
        var telemetryConcrete = sp.GetRequiredService<TelemetryService>();
        Assert.Same(telemetryInterface, telemetryConcrete);
    }

    [Fact]
    public void RepeatedResolutionsReturnSameSingleton()
    {
        var sp = _factory.Services;

        Assert.Same(sp.GetRequiredService<IDeviceService>(), sp.GetRequiredService<IDeviceService>());
        Assert.Same(sp.GetRequiredService<ITelemetryService>(), sp.GetRequiredService<ITelemetryService>());
    }
}
