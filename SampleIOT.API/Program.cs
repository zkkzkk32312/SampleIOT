using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleIOT.API.Services;
using SampleIOT.API.Services.Interface;
using Microsoft.OpenApi.Models;
using System;
using System.Threading.Tasks;

namespace SampleIOT.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register services and middleware before building the app so
            // required services (like ICorsService) are available to the middleware.
            builder.Services.AddSingleton<IDeviceService, DeviceService>();
            builder.Services.AddSingleton<ITelemetryService, TelemetryService>();
            builder.Services.AddControllers();
            // Registers the CORS services required by the CorsMiddleware
            builder.Services.AddCors();
            // Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "SampleIOT API", Version = "v1" }));

            var app = builder.Build();

            // Configure CORS based on environment
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseCors(options =>
                {
                    options.SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrEmpty(origin) || origin.ToLower() == "null")
                            return true;

                        var uri = new Uri(origin);
                        return uri.Host == "localhost" || uri.Host == "127.0.0.1";
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            }
            else
            {
                app.UseCors(options =>
                {
                    options.SetIsOriginAllowedToAllowWildcardSubdomains()
                        .WithOrigins("https://zkkzkk32312.github.io", "https://*.zackcheng.com")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            }

            app.UseHttpsRedirection();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SampleIOT API"));

            // Initialize background services before starting the host
            try
            {
                var deviceServiceInstance = app.Services.GetRequiredService<IDeviceService>();
                var telemetryServiceInstance = app.Services.GetRequiredService<ITelemetryService>();
                await deviceServiceInstance.Start();
                await telemetryServiceInstance.Start();
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to start background services");
            }

            app.Run();
        }


    }
}
