using Microsoft.AspNetCore.Mvc.Testing;
using SampleIOT.API.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SampleIOT.API.Tests.IntegrationTests;

public class TelemetryControllerIntegrationTests : IClassFixture<WebApplicationFactory<SampleIOT.API.Program>>
{
    private readonly HttpClient _client;

    public TelemetryControllerIntegrationTests(WebApplicationFactory<SampleIOT.API.Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TelemetryControllerReturnsNotFoundForUnknownDevice()
    {
        var response = await _client.GetAsync("/api/telemetry/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ExistingDevice_ReturnsOkWithTelemetry()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Telemetry/680539");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var deviceTelemetry = await response.Content.ReadFromJsonAsync<DeviceTelemetry>();

        Assert.NotNull(deviceTelemetry);
        Assert.Equal("680539", deviceTelemetry.Device.Id);
        Assert.True(deviceTelemetry.Telemetries.Length > 0);
    }

    [Fact]
    public async Task Get_WithLimit_ReturnsOnlyLastNEntries()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Telemetry/680539?limit=5");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var deviceTelemetry = await response.Content.ReadFromJsonAsync<DeviceTelemetry>();

        Assert.NotNull(deviceTelemetry);
        Assert.Equal(5, deviceTelemetry.Telemetries.Length);
    }

    [Fact]
    public async Task Get_Disaggregated_ReturnsGroupedByTelemetryKey()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Telemetry/800280?disaggregated=true");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DeviceTelemetry>>();

        Assert.NotNull(result);
        Assert.True(result.Count > 1);

        var keys = result.Select(dt => dt.Telemetries.First().Key).ToList();
        Assert.Equal(keys.Distinct().Count(), keys.Count);
    }

    [Fact]
    public async Task Get_DisaggregatedWithLimit_LimitsPerGroup()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Telemetry/680539?disaggregated=true&limit=3");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DeviceTelemetry>>();

        Assert.NotNull(result);
        Assert.All(result, dt => Assert.InRange(dt.Telemetries.Length, 0, 3));
    }

    [Fact]
    public async Task Get_NegativeLimit_ReturnsBadRequest()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Telemetry/680539?limit=-1");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AltRoute_Get_ExistingDevice_ReturnsOkWithTelemetry()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Telemetry/680539");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var deviceTelemetry = await response.Content.ReadFromJsonAsync<DeviceTelemetry>();

        Assert.NotNull(deviceTelemetry);
        Assert.Equal("680539", deviceTelemetry.Device.Id);
        Assert.True(deviceTelemetry.Telemetries.Length > 0);
    }

    [Fact]
    public async Task AltRoute_Get_NonExistentDevice_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/Telemetry/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
