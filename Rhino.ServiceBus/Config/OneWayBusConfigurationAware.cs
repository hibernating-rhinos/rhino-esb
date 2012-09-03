using System;
using System.Collections.Generic;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Internal;
using System.Messaging;

namespace Rhino.ServiceBus.Config
{
    public class OneWayBusConfigurationAware : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder, IServiceLocator locator)
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
            RegisterMsmqOneWay(config, builder, locator);
        }

        private void RegisterMsmqOneWay(AbstractRhinoServiceBusConfiguration c, IBusContainerBuilder b, IServiceLocator l)
        {
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration)c;

            b.RegisterSingleton<IMessageBuilder<Message>>(() => (IMessageBuilder<Message>)new MsmqMessageBuilder(
                l.Resolve<IMessageSerializer>(),
                l.Resolve<IServiceLocator>()));

            b.RegisterSingleton<IOnewayBus>(() => (IOnewayBus)new MsmqOnewayBus(
                oneWayConfig.MessageOwners,
                l.Resolve<IMessageBuilder<Message>>()));
        }
    }
}