using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleIOT.API.Services;
using SampleIOT.API.Services.Interface;
using System;

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
            //services.AddCors();
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
                    options.AddPolicy("AllowAnyOrigin",
                        builder =>
                        {
                            builder
                                .AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        });
                }
            });

            services.AddControllers();
            services.AddSingleton<IDeviceService, DeviceService>();
            services.AddSingleton<ITelemetryService, TelemetryService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, 
            IDeviceService deviceService, ITelemetryService telemetryService)
        {
            if (env.IsDevelopment())
            {
                app.UseCors("AllowLocalhost");
                //app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseCors("AllowAnyOrigin");
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            deviceService.Start();
            telemetryService.Start();
        }
    }
}
