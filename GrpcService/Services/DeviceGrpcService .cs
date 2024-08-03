using Grpc.Core;
using GrpcService; // Namespace from `option csharp_namespace` in the .proto file
using SampleIOT.API.Models;
using System.Text.Json;

public class DeviceGrpcService : DeviceGrpc.DeviceGrpcBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeviceGrpcService> _logger;

    // Inject DeviceService via constructor
    public DeviceGrpcService(HttpClient httpClient, ILogger<DeviceGrpcService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Implement the RPC method
    public override async Task<DeviceReply> GetDeviceData(DeviceRequest request, ServerCallContext context)
    {
        // Call the API service to get device data
        var response = await _httpClient.GetAsync($"https://localhost:5001/devices/{request.DeviceId}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get device data from API.");
            throw new RpcException(new Status(StatusCode.NotFound, "Device not found"));
        }

        var content = await response.Content.ReadAsStringAsync();
        var device = JsonSerializer.Deserialize<Device>(content);

        return new DeviceReply
        {
            DeviceId = device.Id,
            DeviceType = device.Type,
            TelemetryNames = { device.TelemetryNames }
        };
    }
}
