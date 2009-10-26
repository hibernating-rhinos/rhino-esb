using System;
using System.Collections.Generic;
using Castle.Core;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Serializers;
using System.Linq;

namespace Rhino.ServiceBus.Impl
{
    public class OnewayRhinoServiceBusFacility : AbstractFacility
    {
        private readonly List<MessageOwner> messageOwners = new List<MessageOwner>();
        private Type serializerImpl = typeof(XmlMessageSerializer);

        public void UseMessageSerializer<TMessageSerializer>()
        {
            serializerImpl = typeof(TMessageSerializer);
        }

        protected override void Init()
        {
            new MessageOwnersConfigReader(FacilityConfig, messageOwners).ReadMessageOwners();


            Kernel.Register(
                Component.For<IMessageBuilder>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy<MessageBuilder>(),
                Component.For<IOnewayBus>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy<OnewayBus>()
                    .DependsOn(new{messageOwners = messageOwners.ToArray()}),
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
    }
}