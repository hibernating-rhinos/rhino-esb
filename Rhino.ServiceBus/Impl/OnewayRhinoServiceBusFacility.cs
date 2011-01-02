using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Messaging;
using System.Transactions;
using Castle.Core;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Rhino.Queues;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.RhinoQueues;
using Rhino.ServiceBus.Serializers;
using System.Linq;

namespace Rhino.ServiceBus.Impl
{
    public class OnewayRhinoServiceBusFacility : AbstractFacility
    {
        private readonly List<MessageOwner> messageOwners = new List<MessageOwner>();
        private Type serializerImpl = typeof(XmlMessageSerializer);
        private BusConfigurationSection configuration;

        public void UseMessageSerializer<TMessageSerializer>()
        {
            serializerImpl = typeof(TMessageSerializer);
        }

        private void ReadConfiguration()
        {
            configuration = ConfigurationManager.GetSection("rhino.esb") as BusConfigurationSection;
        }

        protected override void Init()
        {
            ReadConfiguration();
            var messageOwnersReader = new MessageOwnersConfigReader(configuration, messageOwners);
            messageOwnersReader.ReadMessageOwners();
            if (IsRhinoQueues(messageOwnersReader.EndpointScheme))
            {
                Kernel.Register(
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
                Kernel.Register(
                    Component.For<IMessageBuilder<Message>>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy<MsmqMessageBuilder>(),
                    Component.For<IOnewayBus>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy<MsmqOnewayBus>()
                        .DependsOn(new {messageOwners = messageOwners.ToArray()}));
                    
            }
            Kernel.Register(
                Component.For<IReflection>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy<DefaultReflection>(),
                Component.For<IMessageSerializer>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(serializerImpl),
                Component.For<IEndpointRouter>()
                    .ImplementedBy<EndpointRouter>()
                );

        }

        private static bool IsRhinoQueues(string endpointScheme)
        {
            return endpointScheme.Equals("rhino.queues", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}