using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
using Castle.Core;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
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

        public void UseMessageSerializer<TMessageSerializer>()
        {
            serializerImpl = typeof(TMessageSerializer);
        }

        protected override void Init()
        {
            var messageOwnersReader = new MessageOwnersConfigReader(FacilityConfig, messageOwners);
            messageOwnersReader.ReadMessageOwners();
            if (IsRhinoQueues(messageOwnersReader.EndpointScheme))
            {
                var path = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
                Kernel.Register(
                    Component.For<ITransport>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy(typeof (RhinoQueuesTransport))
                        .DependsOn(new
                                       {
                                           threadCount = 1,
                                           endpoint = new Uri("null://nowhere:24689/middle"),
                                           queueIsolationLevel = IsolationLevel.ReadCommitted,
                                           numberOfRetries = 5,
                                           path = Path.Combine(path,"one_way.esent")
                                       }),
                    Component.For<IOnewayBus>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy<RhinoQueuesOneWayBus>()
                        .DependsOn(new {messageOwners = messageOwners.ToArray()})
                    );

            }
            else
            {
                Kernel.Register(
                    Component.For<IMessageBuilder>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy<MsmqMessageBuilder>(),
                    Component.For<IOnewayBus>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy<OnewayBus>()
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