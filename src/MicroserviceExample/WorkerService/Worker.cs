// <copyright file="Worker.cs" company="OpenTelemetry Authors">
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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Utils.Messaging;
using Utils.Messaging.AzureServiceBus;
using Utils.Messaging.RabbitMq;

namespace WorkerService
{
    public partial class Worker : BackgroundService
    {
#if ASB
        private readonly AsbMessageReceiver messageReceiver;
#else
        private readonly RmqMessageReceiver messageReceiver;
#endif

        public Worker(
#if ASB
            AsbMessageReceiver messageReceiver
#else
            RmqMessageReceiver messageReceiver
#endif
            )
        {
            this.messageReceiver = messageReceiver;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            this.messageReceiver.StartConsumer();

            await Task.CompletedTask;
        }
    }
}
