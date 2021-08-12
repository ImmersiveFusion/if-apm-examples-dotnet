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
using IF.APM.Client.Direct;
using IF.APM.OpenTelemetry.Attributes.AspNetCore.Mvc;
using IF.APM.OpenTelemetry.Direct.Trace;
using IF.APM.OpenTelemetry.Rest.Trace;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Utils.Messaging;
using Utils.Messaging.AzureServiceBus;
using Utils.Messaging.RabbitMq;

namespace WebApi
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
            services.AddControllers(options =>
            {
                //adds activities on controller actions
                options.Filters.Add(typeof(ControllerActionActivityFilter));
            });
#if ASB
            services.AddSingleton<AsbMessageSender>();
#else
            services.AddSingleton<RmqMessageSender>();
#endif

            services.AddOpenTelemetryTracing((builder) => builder
                .AddAspNetCoreMvcAttributeSources()
                .AddAspNetCoreInstrumentation()
                .AddSqlClientInstrumentation(options =>
                {
                    options.EnableConnectionLevelAttributes = true;
                    options.SetDbStatementForStoredProcedure = true;
                    options.SetDbStatementForText = true;
                })
#if ASB
                .AddSource(nameof(AsbMessageSender))
#else
                .AddSource(nameof(RmqMessageSender))
#endif
                .AddJaegerExporter(jaegerOptions =>
                {
                    jaegerOptions.AgentHost = "localhost";
                    jaegerOptions.AgentPort = 6831;
                })
                .AddFusionExporter(fusionOptions =>
                {
                    fusionOptions.DirectConnection = new DirectConnectionInformation
                    {
                        Name = "Demo.AspNetCore",
                        Uri = new Uri("amqp://localhost"),
                        Tls = false,
                        IgnoreTlsErrors = true,
                        UserName = "ifdev",
                        Password = "password1",
                        Exchange = "local-dev"
                    };

                })


            );
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
