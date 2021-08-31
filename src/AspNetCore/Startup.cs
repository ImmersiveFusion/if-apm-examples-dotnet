// <copyright file="Startup.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.IO;
using System.Reflection;
using IF.APM.Client.Direct;
using IF.APM.OpenTelemetry.Attributes.AspNetCore.Mvc;
using IF.APM.OpenTelemetry.Direct.Trace;
using IF.APM.OpenTelemetry.Loggers.Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Examples.AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(options =>
            {
                options.ClearProviders();
                options.AddConsole();
                options.AddImmersiveLogger();
            });

            services.AddControllers(options =>
            {
                //adds activities on controller actions
                options.Filters.Add(typeof(ControllerActionActivityFilter));
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            // Switch between Zipkin/Jaeger by setting UseExporter in appsettings.json.
            var exporter = this.Configuration.GetValue<string>("UseExporter").ToLowerInvariant();
            switch (exporter)
            {
                case "jaeger":
                    services.AddOpenTelemetryTracing((builder) => builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(this.Configuration.GetValue<string>("Jaeger:ServiceName")))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreMvcAttributeSources()
                        .AddJaegerExporter(jaegerOptions =>
                        {
                            jaegerOptions.AgentHost = this.Configuration.GetValue<string>("Jaeger:Host");
                            jaegerOptions.AgentPort = this.Configuration.GetValue<int>("Jaeger:Port");
                        })
                        .AddImmersiveExporter(immersiveOptions =>
                        {
                            immersiveOptions.DirectConnection = new DirectConnectionInformation
                            {
                                Name = "Example.AspNetCore",
                                Uri = new Uri("amqp://localhost"),
                                Tls = false,
                                IgnoreTlsErrors = true,
                                UserName = "ifdev",
                                Password = "password1",
                                Exchange = "local-dev"
                            };
                        }));
                    break;
                case "zipkin":
                    services.AddOpenTelemetryTracing((builder) => builder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreMvcAttributeSources()
                        .AddZipkinExporter(zipkinOptions =>
                        {
                            zipkinOptions.Endpoint = new Uri(this.Configuration.GetValue<string>("Zipkin:Endpoint"));
                        })
                        .AddImmersiveExporter(immersiveOptions =>
                        {
                            immersiveOptions.DirectConnection = new DirectConnectionInformation
                            {
                                Name = "Example.AspNetCore",
                                Uri = new Uri("amqp://localhost"),
                                Tls = false,
                                IgnoreTlsErrors = true,
                                UserName = "ifdev",
                                Password = "password1",
                                Exchange = "local-dev"
                            };
                        }));
                    break;
                case "otlp":
                    services.AddOpenTelemetryTracing((builder) => builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(this.Configuration.GetValue<string>("Otlp:ServiceName")))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreMvcAttributeSources()
                        .AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(this.Configuration.GetValue<string>("Otlp:Endpoint"));
                        })
                        .AddImmersiveExporter(immersiveOptions =>
                        {
                            immersiveOptions.DirectConnection = new DirectConnectionInformation
                            {
                                Name = "Example.AspNetCore",
                                Uri = new Uri("amqp://localhost"),
                                Tls = false,
                                IgnoreTlsErrors = true,
                                UserName = "ifdev",
                                Password = "password1",
                                Exchange = "local-dev"
                            };
                        }));
                    break;
                default:
                    services.AddOpenTelemetryTracing((builder) => builder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreMvcAttributeSources()
                        .AddConsoleExporter()
                        .AddImmersiveExporter(immersiveOptions =>
                        {
                            immersiveOptions.DirectConnection = new DirectConnectionInformation
                            {
                                Name = "Example.AspNetCore",
                                Uri = new Uri("amqp://localhost"),
                                Tls = false,
                                IgnoreTlsErrors = true,
                                UserName = "ifdev",
                                Password = "password1",
                                Exchange = "local-dev"
                            };
                        }));
                    break;
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
