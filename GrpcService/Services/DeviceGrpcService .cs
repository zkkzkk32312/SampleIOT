using Grpc.Core;
using GrpcService; // Namespace from `option csharp_namespace` in the .proto file
using SampleIOT.API.Services.Interface;
using System.Threading.Tasks;

public class DeviceGrpcService : DeviceGrpc.DeviceGrpcBase
{
    private readonly IDeviceService _deviceService;

    // Inject DeviceService via constructor
    public DeviceGrpcService(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    // Implement the RPC method
    public override async Task<DeviceReply> GetDeviceData(DeviceRequest request, ServerCallContext context)
    {
        // Use the injected DeviceService to fetch real data
        var device = _deviceService.GetDevice(request.DeviceId);

        // Map the device data to the response format
        var response = new DeviceReply
        {
            DeviceId = device.Id,
            DeviceType = device.Type,
            TelemetryNames = { device.TelemetryNames }
        };

        return response;
    }
}
