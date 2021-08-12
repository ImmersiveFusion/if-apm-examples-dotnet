// <copyright file="RabbitMqHelper.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Utils.Messaging.AzureServiceBus
{
    public static class AsbRabbitMqHelper
    {
        public const string DefaultExchangeName = "";
        public const string TestQueueName = "TestQueue";

        private static readonly string ServiceBusConnectionString;

        static AsbRabbitMqHelper()
        {
            ServiceBusConnectionString = "Endpoint=sample-connection-string-goes-here";
        }
        
        public static IQueueClient CreateModelAndDeclareTestQueue()
        {
            return new QueueClient(ServiceBusConnectionString, TestQueueName);
        }

        public static void StartConsumer(IQueueClient queueClient, Action<Message> processMessage)
        {

            // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = true
            };

            // Register the function that will process messages
            queueClient.RegisterMessageHandler((message, token) =>
            {
                processMessage(message);

                return Task.CompletedTask;
            }, messageHandlerOptions);
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }

        public static void AddMessagingTags(Activity activity)
        {
            // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
            // See:
            //   * https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#messaging-attributes
            //   * https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#rabbitmq
            activity?.SetTag("messaging.system", "azureservicebus");
            activity?.SetTag("messaging.destination_kind", "queue");
            activity?.SetTag("messaging.destination", DefaultExchangeName);
            //activity?.AddTag("messaging.azureservicebus.routing_key", TestQueueName);
        }
    }
}
