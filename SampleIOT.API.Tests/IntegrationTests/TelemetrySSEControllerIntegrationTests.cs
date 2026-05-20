using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Text;

namespace SampleIOT.API.Tests.IntegrationTests;

public class TelemetrySSEControllerIntegrationTests : IClassFixture<WebApplicationFactory<SampleIOT.API.Program>>
{
    private readonly HttpClient _client;

    public TelemetrySSEControllerIntegrationTests(WebApplicationFactory<SampleIOT.API.Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Subscribe_ReturnsSSEHeaders()
    {
        await WithTimeout(async () =>
        {
            using var cts = new CancellationTokenSource();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/TelemetrySSE/Subscribe/680539");
            request.Version = new Version(1, 1);

            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
            Assert.True(response.Headers.TryGetValues("Cache-Control", out var cacheValues));
            Assert.Contains("no-cache", cacheValues);

            cts.Cancel();
        }, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Subscribe_ConnectionHoldsOpen()
    {
        await WithTimeout(async () =>
        {
            using var cts = new CancellationTokenSource();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/TelemetrySSE/Subscribe/680539");
            request.Version = new Version(1, 1);

            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var stream = await response.Content.ReadAsStreamAsync();
            var initial = await ReadChunkAsync(stream, TimeSpan.FromSeconds(2));
            Assert.Contains("Client ID:", initial);

            await Task.Delay(3000);

            var buffer = new byte[1024];
            var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
            Assert.False(readTask.IsCompleted);

            cts.Cancel();
        }, TimeSpan.FromSeconds(15));
    }

    static async Task WithTimeout(Func<Task> action, TimeSpan timeout)
    {
        var task = action();
        if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            await task;
    }

    static async Task<string> ReadChunkAsync(Stream stream, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var buffer = new byte[8192];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
        return bytesRead > 0 ? Encoding.UTF8.GetString(buffer, 0, bytesRead) : string.Empty;
    }
}
