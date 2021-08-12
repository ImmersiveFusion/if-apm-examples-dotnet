// <copyright file="Program.cs" company="OpenTelemetry Authors">
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
using IF.APM.OpenTelemetry.Direct.Trace;
using IF.APM.OpenTelemetry.Rest.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using Utils.Messaging.AzureServiceBus;
using Utils.Messaging.RabbitMq;

namespace WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {

                    RedisMux = ConnectionMultiplexer.Connect("localhost");

                    services.AddSingleton<IConnectionMultiplexer>(RedisMux);
                    services.AddHostedService<Worker>();
#if ASB
                    services.AddSingleton<AsbMessageReceiver>();
#else
                    services.AddSingleton<RmqMessageReceiver>();
#endif
                    services.AddOpenTelemetryTracing((builder) =>
                    {
                        builder
#if ASB
                            .AddSource(nameof(AsbMessageReceiver))
#else
                            .AddSource(nameof(RmqMessageReceiver))
#endif
                            .AddSqlClientInstrumentation(options =>
                            {
                                options.EnableConnectionLevelAttributes = true;
                                options.SetDbStatementForStoredProcedure = true;
                                options.SetDbStatementForText = true;
                            })
                            .AddRedisInstrumentation(RedisMux)
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
                            });
                    });
                });

        public static ConnectionMultiplexer RedisMux { get; set; }
    }
}
