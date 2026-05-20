using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SampleIOT.API.Tests.IntegrationTests;

public class ProgramIntegrationTests : IClassFixture<WebApplicationFactory<SampleIOT.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<SampleIOT.API.Program> _factory;

    public ProgramIntegrationTests(WebApplicationFactory<SampleIOT.API.Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string? GetCorsOrigin(HttpResponseHeaders headers)
    {
        return headers.TryGetValues("Access-Control-Allow-Origin", out var values)
            ? values.FirstOrDefault()
            : null;
    }

    private static WebApplicationFactory<SampleIOT.API.Program> CreateFactory(string environment)
    {
        return new WebApplicationFactory<SampleIOT.API.Program>()
            .WithWebHostBuilder(builder =>
                builder.UseSetting("environment", environment));
    }

    // ----------------------------
    // BASIC ROUTES
    // ----------------------------

    [Fact]
    public async Task RootRedirectsToSwagger()
    {
        var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/swagger", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task SwaggerJsonEndpointReturnsOk()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("paths", content);
    }

    [Fact]
    public async Task SwaggerUIReturnsOk()
    {
        var response = await _client.GetAsync("/swagger");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("swagger", content);
    }

    // ----------------------------
    // CORS TESTS
    // ----------------------------

    [Theory]
    [InlineData("http://localhost:3000")]
    [InlineData("http://127.0.0.1:8080")]
    public async Task DevPolicy_AllowsLocalhostOrigins(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Device");
        request.Headers.Add("Origin", origin);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(origin, GetCorsOrigin(response.Headers));
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.Equal("true",
            response.Headers.GetValues("Access-Control-Allow-Credentials").First());
    }

    [Fact]
    public async Task DevPolicy_BlocksExternalOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Device");
        request.Headers.Add("Origin", "http://evil.com");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(GetCorsOrigin(response.Headers));
    }

    [Fact]
    public async Task PreflightRequest_ReturnsCorsHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/Device");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:3000", GetCorsOrigin(response.Headers));
        Assert.True(response.Headers.Contains("Access-Control-Allow-Methods"));
        Assert.Contains("GET", response.Headers.GetValues("Access-Control-Allow-Methods"));
        Assert.True(response.Headers.Contains("Access-Control-Allow-Headers"));
        Assert.Contains("Content-Type", response.Headers.GetValues("Access-Control-Allow-Headers"));
        Assert.True(response.Headers.Contains("Access-Control-Max-Age"));
    }

    [Fact]
    public async Task PreflightRequest_BlockedOrigin_ReturnsNoCorsHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/Device");
        request.Headers.Add("Origin", "http://evil.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        Assert.Null(GetCorsOrigin(response.Headers));
    }

    [Fact]
    public async Task DevPolicy_VaryHeader_SetForOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Device");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await _client.SendAsync(request);

        Assert.Contains("Origin", response.Headers.Vary);
    }

    // ----------------------------
    // ENVIRONMENT TESTS
    // ----------------------------

    [Theory]
    [InlineData("Production", "https://zkkzkk32312.github.io")]
    [InlineData("Production", "https://app.zackcheng.com")]
    public async Task ProdEnvironment_AllowsProdOrigins(string environment, string origin)
    {
        using var factory = CreateFactory(environment);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Device");
        request.Headers.Add("Origin", origin);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(origin, GetCorsOrigin(response.Headers));
    }

    [Fact]
    public async Task ProdEnvironment_BlocksLocalhostOrigin()
    {
        using var factory = CreateFactory(Environments.Production);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Device");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(GetCorsOrigin(response.Headers));
    }

    [Fact]
    public async Task ProdPolicy_BlocksMaliciousGithubSubdomain()
    {
        using var factory = CreateFactory(Environments.Production);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Device");
        request.Headers.Add("Origin", "https://zkkzkk32312.github.io.evil.com");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(GetCorsOrigin(response.Headers));
    }

    // ----------------------------
    // ERROR TESTS
    // ----------------------------

    [Fact]
    public async Task DevEnvironment_ErrorResponseContainsTraceId()
    {
        using var factory = CreateFactory(Environments.Development);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/telemetry/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("traceId", content);
    }
}