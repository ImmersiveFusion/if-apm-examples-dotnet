// <copyright file="SendMessageController.cs" company="OpenTelemetry Authors">
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Utils.Messaging;
using Utils.Messaging.AzureServiceBus;
using Utils.Messaging.RabbitMq;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendMessageController : ControllerBase
    {
        private readonly ILogger<SendMessageController> logger;
#if ASB
        private readonly AsbMessageSender messageSender;
#else
        private readonly RmqMessageSender messageSender;
#endif

        public SendMessageController(ILogger<SendMessageController> logger,
#if ASB
            AsbMessageSender messageSender
#else
            RmqMessageSender messageSender
#endif

            )
        {
            this.logger = logger;
            this.messageSender = messageSender;
        }

        [HttpGet]
        public IActionResult Get()
        {
            this.messageSender.SendMessage();
            return Ok("Message sent");
        }
    }
}
