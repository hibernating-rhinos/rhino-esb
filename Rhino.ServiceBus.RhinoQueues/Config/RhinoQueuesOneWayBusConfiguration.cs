using System;
using System.Collections.Generic;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Config
{
    public class RhinoQueuesOneWayBusConfiguration : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder)
        {
            var oneWayConfig = config as OnewayRhinoServiceBusConfiguration;
            if (oneWayConfig == null)
                return;

            var containerBuilder = builder as IRhinoQueuesBusContainerBuilder;
            if (containerBuilder == null)
                return;

            var messageOwners = new List<MessageOwner>();
            var messageOwnersReader = new MessageOwnersConfigReader(config.ConfigurationSection, messageOwners);
            messageOwnersReader.ReadMessageOwners();
            oneWayConfig.MessageOwners = messageOwners.ToArray();
            if (messageOwnersReader.EndpointScheme.Equals("rhino.queues", StringComparison.InvariantCultureIgnoreCase) == false)
                return;

            containerBuilder.RegisterRhinoQueuesOneWay();
        }
    }
}