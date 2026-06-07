using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    public void Services_AreTrueSingletons()
    {
        var sp = _factory.Services;

        Assert.Same(sp.GetRequiredService<IDeviceService>(), sp.GetRequiredService<IDeviceService>());
        Assert.Same(sp.GetRequiredService<ITelemetryService>(), sp.GetRequiredService<ITelemetryService>());
    }

    [Fact]
    public void HostedServiceInstance_IsSameAsInterfaceResolvedInstance()
    {
        var sp = _factory.Services;

        var deviceViaInterface = sp.GetRequiredService<IDeviceService>();
        var telemetryViaInterface = sp.GetRequiredService<ITelemetryService>();

        var hostedServices = sp.GetRequiredService<IEnumerable<IHostedService>>();
        var deviceHosted = hostedServices.OfType<DeviceService>().Single();
        var telemetryHosted = hostedServices.OfType<TelemetryService>().Single();

        Assert.Same(deviceViaInterface, deviceHosted);
        Assert.Same(telemetryViaInterface, telemetryHosted);
    }
}
