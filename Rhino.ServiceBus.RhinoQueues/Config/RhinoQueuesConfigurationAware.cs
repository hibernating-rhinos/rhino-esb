using System;
using System.Configuration;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.RhinoQueues;
using Rhino.Queues;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Config
{
    public class RhinoQueuesConfigurationAware : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder, IServiceLocator locator)
        {
            var busConfig = config as RhinoServiceBusConfiguration;
            if (busConfig == null)
                return;

            if (!config.Endpoint.Scheme.Equals("rhino.queues", StringComparison.InvariantCultureIgnoreCase))
                return;

            var busConfigSection = config.ConfigurationSection.Bus;

            if (string.IsNullOrEmpty(busConfigSection.Name))
                throw new ConfigurationErrorsException(
                    "Could not find attribute 'name' in node 'bus' in configuration");

            RegisterRhinoQueuesTransport(config, builder, locator);
        }

        private void RegisterRhinoQueuesTransport(AbstractRhinoServiceBusConfiguration c, IBusContainerBuilder b, IServiceLocator l)
        {
            var busConfig = c.ConfigurationSection.Bus;
            var queueManagerConfiguration = new QueueManagerConfiguration();

            b.RegisterSingleton<ISubscriptionStorage>(() => (ISubscriptionStorage)new PhtSubscriptionStorage(
                busConfig.SubscriptionPath,
                l.Resolve<IMessageSerializer>(),
                l.Resolve<IReflection>()));

            b.RegisterSingleton<ITransport>(() => (ITransport)new RhinoQueuesTransport(
                c.Endpoint,
                l.Resolve<IEndpointRouter>(),
                l.Resolve<IMessageSerializer>(),
                c.ThreadCount,
                busConfig.QueuePath,
                c.IsolationLevel,
                c.NumberOfRetries,
                busConfig.EnablePerformanceCounters,
                l.Resolve<IMessageBuilder<MessagePayload>>(),
                queueManagerConfiguration));

            b.RegisterSingleton<IMessageBuilder<MessagePayload>>(() => (IMessageBuilder<MessagePayload>)new RhinoQueuesMessageBuilder(
                l.Resolve<IMessageSerializer>(),
                l.Resolve<IServiceLocator>()));

            b.RegisterSingleton<QueueManagerConfiguration>(() => queueManagerConfiguration);
        }
    }
}