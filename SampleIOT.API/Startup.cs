using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using SampleIOT.API.Services;
using SampleIOT.API.Services.Interface;
using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SampleIOT.API
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            services.AddCors(options =>
            {
                if (_env.IsDevelopment())
                {
                    options.AddPolicy("AllowLocalhost",
                        builder =>
                        {
                            builder.SetIsOriginAllowed(origin =>
                            {
                                using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                                var logger = loggerFactory.CreateLogger<Startup>();

                                logger.LogInformation($"Dev CORS check - Origin: '{origin}'");

                                // If origin is null or empty, treat it as a local request
                                if (string.IsNullOrEmpty(origin) || origin.ToLower() == "null")
                                {
                                    logger.LogInformation("Allowing null/empty origin");
                                    return true;
                                }

                                var uri = new Uri(origin);
                                var allowed = uri.Host == "localhost" || uri.Host == "127.0.0.1";
                                logger.LogInformation($"Origin {origin} - Allowed: {allowed}");
                                return allowed;
                            })
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .SetPreflightMaxAge(TimeSpan.FromMinutes(30));
                        });
                }
                else
                {
                    options.AddPolicy("AllowMyDomain",
                        builder =>
                        {
                            builder
                                .SetIsOriginAllowed(origin =>
                                {
                                    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                                    var logger = loggerFactory.CreateLogger<Startup>();

                                    logger.LogInformation($"Prod CORS check - Origin: '{origin}', Environment: {_env.EnvironmentName}");

                                    if (origin?.StartsWith("https://zkkzkk32312.github.io") == true)
                                    {
                                        logger.LogInformation("GitHub Pages origin allowed");
                                        return true;
                                    }
                                    if (origin?.StartsWith("https://") == true && origin.EndsWith(".zackcheng.com"))
                                    {
                                        logger.LogInformation("Zackcheng.com origin allowed");
                                        return true;
                                    }

                                    logger.LogWarning($"Origin rejected: {origin}");
                                    return false;
                                })
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials()
                                .SetPreflightMaxAge(TimeSpan.FromMinutes(30));
                        });
                }
            });

            services.AddControllers();
            services.AddSwaggerGen();
            services.AddSingleton<IDeviceService, DeviceService>();
            services.AddSingleton<ITelemetryService, TelemetryService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IDeviceService deviceService, ITelemetryService telemetryService)
        {
            app.UseStaticFiles();

            app.UseHttpsRedirection();

            app.UseRouting();

            if (env.IsDevelopment())
            {
                app.UseCors("AllowLocalhost");
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseCors("AllowMyDomain");
            }

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

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SampleIOT.API v1");
                c.RoutePrefix = "swagger";
            });

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    await deviceService.Start();
                    await telemetryService.Start();
                }
                catch (Exception ex)
                {
                    // Log the error but don't crash the application
                    var logger = app.ApplicationServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Failed to start background services");
                }
            });
        }
    }
}
