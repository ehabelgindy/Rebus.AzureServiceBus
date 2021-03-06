﻿using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Rebus.Extensions;
using Rebus.Internals;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests.Factories
{
    public class AzureServiceBusTransportFactory : ITransportFactory
    {
        readonly Dictionary<string, AzureServiceBusTransport> _queuesToDelete = new Dictionary<string, AzureServiceBusTransport>();

        public ITransport CreateOneWayClient()
        {
            return Create(null);
        }

        public ITransport Create(string inputQueueAddress)
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var asyncTaskFactory = new TplAsyncTaskFactory(consoleLoggerFactory);

            if (inputQueueAddress == null)
            {
                var transport = new AzureServiceBusTransport(AsbTestConfig.ConnectionString, null, consoleLoggerFactory, asyncTaskFactory, new AzureServiceBusNameHelper());

                transport.Initialize();

                return transport;
            }

            return _queuesToDelete.GetOrAdd(inputQueueAddress, () =>
            {
                var transport = new AzureServiceBusTransport(AsbTestConfig.ConnectionString, inputQueueAddress, consoleLoggerFactory, asyncTaskFactory, new AzureServiceBusNameHelper());

                transport.PurgeInputQueue();

                transport.Initialize();

                return transport;
            });
        }

        public void CleanUp()
        {
            _queuesToDelete.Keys.ForEach(DeleteQueue);

            _queuesToDelete.Values.ForEach(t => t.Dispose());
        }

        public static void DeleteQueue(string queueName)
        {
            AsyncHelpers.RunSync(async () =>
            {
                var managementClient = new ManagementClient(AsbTestConfig.ConnectionString);

                try
                {
                    Console.Write("Deleting ASB queue {0}...", queueName);
                    await managementClient.DeleteQueueAsync(queueName);
                    Console.WriteLine("OK!");
                }
                catch (MessagingEntityNotFoundException)
                {
                    Console.WriteLine("OK (was not there)");
                }
            });
        }

        public static void DeleteTopic(string topic)
        {
            AsyncHelpers.RunSync(async () =>
            {
                var managementClient = new ManagementClient(AsbTestConfig.ConnectionString);

                try
                {
                    Console.Write("Deleting topic '{0}' ...", topic);
                    await managementClient.DeleteTopicAsync(topic);
                    Console.WriteLine("OK!");
                }
                catch (MessagingEntityNotFoundException)
                {
                    Console.WriteLine("OK! (wasn't even there)");
                }
            });
        }
    }
}
