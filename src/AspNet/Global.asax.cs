// <copyright file="Global.asax.cs" company="OpenTelemetry Authors">
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
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using IF.APM.Client.Direct;
using IF.APM.OpenTelemetry;
using IF.APM.OpenTelemetry.Direct.Trace;
using IF.APM.OpenTelemetry.Rest.Trace;
using IF.APM.OpenTelemetry.Trace;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Examples.AspNet
{
#pragma warning disable SA1649 // File name should match first type name
    public class WebApiApplication : HttpApplication
#pragma warning restore SA1649 // File name should match first type name
    {
        private IDisposable tracerProvider;

        protected void Application_Start()
        {
            this.tracerProvider = Sdk.CreateTracerProviderBuilder()
                 .AddHttpClientInstrumentation()
                 .AddAspNetInstrumentation()
                 .AddAspNetMvcAttributeSources()
                 .AddJaegerExporter(jaegerOptions =>
                 {
                     jaegerOptions.AgentHost = "localhost";
                     jaegerOptions.AgentPort = 6831;
                 })
                 .AddImmersiveExporter(immersiveOptions =>
                 {
                     immersiveOptions.DirectConnection = new DirectConnectionInformation
                     {
                         Name = "Example.AspNet",
                         Uri = new Uri("amqp://localhost"),
                         Tls = false,
                         IgnoreTlsErrors = true,
                         UserName = "ifdev",
                         Password = "password1",
                         Exchange = "local-dev"
                     };
                 })
                 .Build();

            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_End()
        {
            this.tracerProvider?.Dispose();
        }
    }
}
