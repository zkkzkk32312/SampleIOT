using GrpcService.Services;
using SampleIOT.API.Services;
using SampleIOT.API.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

// Register HttpClient
builder.Services.AddHttpClient();
// Add gRPC services to the container.
builder.Services.AddGrpc();
//builder.Services.AddSingleton<IDeviceService, DeviceService>();

var app = builder.Build();

await Task.Delay(3000);

// Configure the HTTP request pipeline.
app.UseRouting();

// Enable gRPC-Web support
app.UseGrpcWeb();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<GreeterService>().EnableGrpcWeb();
    endpoints.MapGrpcService<DeviceGrpcService>().EnableGrpcWeb(); // Enable gRPC-Web for this service
    endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");
});

app.Run();