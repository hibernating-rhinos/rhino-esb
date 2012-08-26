using System;
using System.Collections.Generic;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Config
{
    public class OneWayBusConfiguration : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder)
        {
            var oneWayConfig = config as OnewayRhinoServiceBusConfiguration;
            if (oneWayConfig == null)
                return;

            var messageOwners = new List<MessageOwner>();
            var messageOwnersReader = new MessageOwnersConfigReader(config.ConfigurationSection, messageOwners);
            messageOwnersReader.ReadMessageOwners();

            if (!messageOwnersReader.EndpointScheme.Equals("msmq", StringComparison.InvariantCultureIgnoreCase))
                return;

            oneWayConfig.MessageOwners = messageOwners.ToArray();
            builder.RegisterMsmqOneWay();
        }
    }
}