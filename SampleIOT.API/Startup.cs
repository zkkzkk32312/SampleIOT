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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
                                // If origin is null or empty, treat it as a local request
                                if (string.IsNullOrEmpty(origin) || origin.ToLower() == "null")
                                {
                                    return true;
                                }

                                var uri = new Uri(origin);
                                var allowed = uri.Host == "localhost" || uri.Host == "127.0.0.1";
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
                                    if (origin?.StartsWith("https://zkkzkk32312.github.io") == true)
                                    {
                                        return true;
                                    }
                                    if (origin?.StartsWith("https://") == true && origin.EndsWith(".zackcheng.com"))
                                    {
                                        return true;
                                    }

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

            // IMPORTANT: Comment out or remove UseHttpsRedirection when behind a reverse proxy
            // like Nginx Proxy Manager that handles SSL termination and forwards HTTP to the app.
            // If you need to keep it for other scenarios, ensure UseForwardedHeaders is correctly configured.
            // app.UseHttpsRedirection(); 

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