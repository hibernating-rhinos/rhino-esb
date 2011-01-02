using System;
using System.IO;
using System.Transactions;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.Queues;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.RhinoQueues;

namespace Rhino.ServiceBus.Castle.Config
{
    public class CastleRhinoQueuesConfiguration : RhinoQueuesConfigurationAware 
    {
        private readonly IWindsorContainer container;

        public CastleRhinoQueuesConfiguration(IWindsorContainer container)
        {
            this.container = container;
        }

        protected override void RegisterTransportServices(int threadCount, Uri endpoint, IsolationLevel queueIsolationLevel, int numberOfRetries, string path, string name)
        {
            container.Register(
                Component.For<ISubscriptionStorage>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(PhtSubscriptionStorage))
                    .DependsOn(new
                    {
                        subscriptionPath = Path.Combine(path, name + "_subscriptions.esent")
                    }),
                Component.For<ITransport>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(RhinoQueuesTransport))
                    .DependsOn(new
                    {
                        threadCount,
                        endpoint,
                        queueIsolationLevel,
                        numberOfRetries,
                        path = Path.Combine(path, name + ".esent")
                    }),
                Component.For<IMessageBuilder<MessagePayload>>()
                    .ImplementedBy<RhinoQueuesMessageBuilder>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                );
        }
    }
}