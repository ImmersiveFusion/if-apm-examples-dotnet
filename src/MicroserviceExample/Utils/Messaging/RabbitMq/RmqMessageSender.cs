// <copyright file="RmqMessageSender.cs" company="OpenTelemetry Authors">
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
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using RabbitMQ.Client;

namespace Utils.Messaging.RabbitMq
{
    public class RmqMessageSender : IDisposable
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(RmqMessageSender));
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        private readonly ILogger<RmqMessageSender> logger;
        private readonly IConnection connection;
        private readonly IModel channel;

        public RmqMessageSender(ILogger<RmqMessageSender> logger)
        {
            this.logger = logger;
            this.connection = RmqRabbitMqHelper.CreateConnection();
            this.channel = RmqRabbitMqHelper.CreateModelAndDeclareTestQueue(this.connection);
        }

        public void Dispose()
        {
            this.channel.Dispose();
            this.connection.Dispose();
        }

        public void SendMessage()
        {
            // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
            // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#span-name
            var activityName = $"{RmqRabbitMqHelper.TestQueueName}";

            using (var activity = ActivitySource.StartActivity(activityName, ActivityKind.Producer))
            {
                try
                {
                    var props = this.channel.CreateBasicProperties();

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

                    // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
                    Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), props,
                        this.InjectTraceContextIntoBasicProperties);

                    // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
                    RmqRabbitMqHelper.AddMessagingTags(activity);
                    var body = $"Published message: DateTime.Now = {DateTime.Now}.";

                    this.channel.BasicPublish(
                        exchange: RmqRabbitMqHelper.DefaultExchangeName,
                        routingKey: RmqRabbitMqHelper.TestQueueName,
                        basicProperties: props,
                        body: Encoding.UTF8.GetBytes(body));

                    this.logger.LogInformation($"Message sent: [{body}]");

                    activity.AddEvent(new ActivityEvent("Sent message",
                        tags: new ActivityTagsCollection(new[] { new KeyValuePair<string, object>("etag", "evalue") })));
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Message publishing failed.");
                    activity.SetStatus(Status.Unset.WithDescription(ex.Message));
                    activity.RecordException(ex);
                }
            }
        }

        private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
        {
            try
            {
                if (props.Headers == null)
                {
                    props.Headers = new Dictionary<string, object>();
                }

                props.Headers[key] = value;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to inject trace context.");
            }
        }
    }
}
