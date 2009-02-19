using System;
using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Msmq;
using Xunit;
using System.Linq;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class Can_read_subscriptions_from_queue : MsmqTestBase
    {
        private static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            return container;
        }

        [Fact]
        public void when_subscribed_and_bus_is_closed_and_then_restarted()
        {
            var container = CreateContainer();

            var subscriptionChanged = new ManualResetEvent(false);
            IStartableServiceBus bus;
            using(bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
                ((AbstractMsmqListener) container.Resolve<ITransport>()).MessageMoved += 
                    () => subscriptionChanged.Set();
                bus.Send(bus.Endpoint,new AddSubscription
                {
                    Endpoint = bus.Endpoint,
                    Type = typeof(int).FullName
                });

                subscriptionChanged.WaitOne(TimeSpan.FromSeconds(30), false);
            }

            var container2 = CreateContainer();
            using(var bus2 = container2.Resolve<IStartableServiceBus>())
            {
                Assert.NotSame(bus, bus2);

                bus2.Start();

                var subscriptionStorage2 = container2.Resolve<ISubscriptionStorage>();

                var subscriptionsFor = subscriptionStorage2.GetSubscriptionsFor(typeof(int))
                    .ToArray();

                Assert.Equal(bus2.Endpoint.Uri, subscriptionsFor[0]);
            }
        }


    }
}