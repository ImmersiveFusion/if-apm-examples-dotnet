// <copyright file="MessageSender.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace Utils.Messaging.AzureServiceBus
{
    public class AsbMessageSender : IDisposable
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(AsbMessageSender));
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        private readonly ILogger<AsbMessageSender> logger;
        private readonly IQueueClient queueClient;

        public AsbMessageSender(ILogger<AsbMessageSender> logger)
        {
            this.logger = logger;
            this.queueClient = AsbRabbitMqHelper.CreateModelAndDeclareTestQueue();
        }

        public void Dispose()
        {
            this.queueClient.CloseAsync().GetAwaiter().GetResult();
        }

        public void SendMessage()
        {

            // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
            // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#span-name
            var activityName = $"{AsbRabbitMqHelper.TestQueueName}";

            using (var activity = ActivitySource.StartActivity(activityName, ActivityKind.Producer))
            {
                try
                {
                    // Depending on Sampling (and whether a listener is registered or not), the
                    // activity above may not be created.
                    // If it is created, then propagate its context.
                    // If it is not created, the propagate the Current context,
                    // if any.
                    ActivityContext contextToInject = default;
                    if (activity != null)
                    {
                        contextToInject = activity.Context;
                    }
                    else if (Activity.Current != null)
                    {
                        contextToInject = Activity.Current.Context;
                    }

                    var body = $"Published message: DateTime.Now = {DateTime.Now}.";

                    var message = new Message(Encoding.UTF8.GetBytes(body));
                    // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
                    Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), message.UserProperties, this.InjectTraceContextIntoMessage);

                    // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
                    AsbRabbitMqHelper.AddMessagingTags(activity);

                    this.queueClient.SendAsync(message).GetAwaiter().GetResult();

                    this.logger.LogInformation($"Message sent: [{body}]");

                    activity.AddEvent(new ActivityEvent("Sent message",
                        tags: new ActivityTagsCollection(new[] { new KeyValuePair<string, object>("etag", "evalue"), })));
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Message publishing failed.");
                    activity.SetStatus(Status.Unset.WithDescription(ex.Message));
                    activity.RecordException(ex);
                }
            }
        }

        private void InjectTraceContextIntoMessage(IDictionary<string, object> props, string key, string value)
        {
            try
            {
                props[key] = value;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to inject trace context.");
            }
        }
    }
}
