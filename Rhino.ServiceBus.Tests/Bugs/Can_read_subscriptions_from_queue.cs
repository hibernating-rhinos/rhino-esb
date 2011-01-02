using System;
using System.Messaging;
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
            new RhinoServiceBusFacility()
                .UseCastleWindsor(container)
                .Configure();
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

		[Fact]
		public void add_subscription_is_sent_at_high_priority()
		{
			var container = CreateContainer();

			using (var bus = container.Resolve<IStartableServiceBus>())
			{
				bus.Start();
				bus.Send(TestQueueUri2, new AddSubscription
				{
					Endpoint = bus.Endpoint,
					Type = typeof(int).FullName
				});
			}

			testQueue2.MessageReadPropertyFilter.SetAll();
			var message = testQueue2.Peek(TimeSpan.FromMilliseconds(250));
			Assert.Equal("Rhino.ServiceBus.Messages.AddSubscription", message.Label);
			Assert.Equal(MessagePriority.High, message.Priority);
		}

		[Fact]
		public void remove_subscription_is_sent_at_high_priority()
		{
			var container = CreateContainer();

			using (var bus = container.Resolve<IStartableServiceBus>())
			{
				bus.Start();
				bus.Send(TestQueueUri2, new RemoveSubscription
				{
					Endpoint = bus.Endpoint,
					Type = typeof(int).FullName
				});
			}

			testQueue2.MessageReadPropertyFilter.SetAll();
			var message = testQueue2.Peek(TimeSpan.FromMilliseconds(250));
			Assert.Equal("Rhino.ServiceBus.Messages.RemoveSubscription", message.Label);
			Assert.Equal(MessagePriority.High, message.Priority);
		}
    }
}