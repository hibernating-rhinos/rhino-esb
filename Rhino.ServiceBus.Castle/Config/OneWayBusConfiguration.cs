using System;
using System.Collections.Generic;
using System.Messaging;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.Queues;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.RhinoQueues;

namespace Rhino.ServiceBus.Castle.Config
{
    public class OneWayBusConfiguration : IBusConfigurationAware
    {
        private readonly IWindsorContainer container;

        public OneWayBusConfiguration(IWindsorContainer container)
        {
            this.container = container;
        }

        public void Configure(AbstractRhinoServiceBusFacility config)
        {
            var oneWayConfig = config as OnewayRhinoServiceBusFacility;
            if (oneWayConfig == null)
                return;

            var messageOwners = new List<MessageOwner>();
            var messageOwnersReader = new MessageOwnersConfigReader(config.ConfigurationSection, messageOwners);
            messageOwnersReader.ReadMessageOwners();
            if (IsRhinoQueues(messageOwnersReader.EndpointScheme))
            {
                container.Register(
                     Component.For<IMessageBuilder<MessagePayload>>()
                        .ImplementedBy<RhinoQueuesMessageBuilder>()
                        .LifeStyle.Is(LifestyleType.Singleton),   
                    Component.For<IOnewayBus>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy<RhinoQueuesOneWayBus>()
                        .DependsOn(new
                                       {
                                           messageOwners = messageOwners.ToArray(),
                                       })
                    );
            }
            else
            {
                container.Register(
                    Component.For<IMessageBuilder<Message>>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy<MsmqMessageBuilder>(),
                    Component.For<IOnewayBus>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy<MsmqOnewayBus>()
                        .DependsOn(new {messageOwners = messageOwners.ToArray()}));
                    
            }
        }

        private static bool IsRhinoQueues(string endpointScheme)
        {
            return endpointScheme.Equals("rhino.queues", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}