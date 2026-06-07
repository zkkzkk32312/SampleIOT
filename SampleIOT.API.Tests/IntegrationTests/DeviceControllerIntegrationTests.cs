using Microsoft.AspNetCore.Mvc.Testing;
using SampleIOT.API.Models;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SampleIOT.API.Tests.IntegrationTests;

public class DeviceControllerIntegrationTests : IClassFixture<WebApplicationFactory<SampleIOT.API.Program>>
{
    private readonly HttpClient _client;

    public DeviceControllerIntegrationTests(WebApplicationFactory<SampleIOT.API.Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DevicesController_ReturnsJsonContentType()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Device");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DevicesControllerAltRoute_ReturnsHtmlContentType()
    {
        var response = await _client.GetAsync("/Devices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetDevice_ExistingId_ReturnsOk()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Device/680539");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var device = await response.Content.ReadFromJsonAsync<Device>();
        Assert.NotNull(device);
        Assert.Equal("680539", device.Id);
    }

    [Fact]
    public async Task GetDevice_NonExistentId_ReturnsNotFound()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Device/999999");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_SortById_ReturnsSortedDevices()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Device?sort=id");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var devices = JsonSerializer.Deserialize<List<Device>>(content);

        Assert.NotNull(devices);
        Assert.True(devices.Count > 1);

        var ids = devices.Select(d => d.Id).ToList();
        for (int i = 0; i < ids.Count - 1; i++)
        {
            Assert.True(string.Compare(ids[i], ids[i + 1], StringComparison.OrdinalIgnoreCase) <= 0,
                $"Device ID '{ids[i]}' should come before '{ids[i + 1]}'");
        }
    }
}
