using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SampleIOT.API.Services;
using Microsoft.AspNetCore.Http;
using System;

namespace SampleIOT.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<DeviceService>();
            builder.Services.AddSingleton<IDeviceService>(sp => sp.GetRequiredService<DeviceService>());
            builder.Services.AddSingleton<TelemetryService>();
            builder.Services.AddSingleton<ITelemetryService>(sp => sp.GetRequiredService<TelemetryService>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<DeviceService>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<TelemetryService>());
            builder.Services.AddControllers();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DevPolicy", policy =>
                    policy.SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrEmpty(origin) || origin.ToLower() == "null")
                            return true;
                        var uri = new Uri(origin);
                        return uri.Host == "localhost" || uri.Host == "127.0.0.1";
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(30)));

                options.AddPolicy("ProdPolicy", policy =>
                    policy.SetIsOriginAllowed(origin =>
                    {
                        if (origin == "https://zkkzkk32312.github.io")
                            return true;
                        var uri = new Uri(origin);
                        if (uri.Scheme == "https" && (uri.Host == "zackcheng.com" || uri.Host.EndsWith(".zackcheng.com")))
                            return true;
                        return false;
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(30)));
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "SampleIOT API", Version = "v1" }));

            var app = builder.Build();

            // UseHttpsRedirection is disabled because the app runs behind a reverse proxy
            // (Nginx Proxy Manager) that handles SSL termination and forwards HTTP to the app.
            // Enabling it would cause 301 redirects on preflight OPTIONS requests,
            // breaking CORS because the redirect response lacks CORS headers.
            app.UseStaticFiles();

            app.UseRouting();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors("DevPolicy");
            }
            else
            {
                app.UseCors("ProdPolicy");
            }

            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SampleIOT API"));

            // Redirect root "/" to "/swagger"
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.Redirect("/swagger");
                    return;
                }
                await next();
            });

            app.UseEndpoints(endpoints => endpoints.MapControllers());

            app.Run();
        }
    }
}
