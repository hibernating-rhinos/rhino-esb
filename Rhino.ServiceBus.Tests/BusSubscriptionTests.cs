using System;
using System.Linq;
using System.Messaging;
using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class BusSubscriptionTests : MsmqTestBase
    {
        private readonly IWindsorContainer container;

        public BusSubscriptionTests()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
        }

        [Fact]
        public void Adding_subscription_that_already_exists_has_no_op_on_queue()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                var storage = container.Resolve<ISubscriptionStorage>();
                var listener = (AbstractMsmqListener)container.Resolve<ITransport>();
                var waitSubscriptions = new ManualResetEvent(false);
                var waitListener = new ManualResetEvent(false);
                int count = 0;
                listener.MessageMoved += () => waitListener.Set();
                storage.SubscriptionChanged += () =>
                {
                    if (Interlocked.Increment(ref count) == 2)
                        waitSubscriptions.Set();
                };

                bus.Subscribe<MsmqSubscriptionTests.TestMessage>();
                bus.Subscribe<MsmqSubscriptionTests.TestMessage>();

                waitListener.WaitOne(TimeSpan.FromSeconds(30));
                waitSubscriptions.WaitOne(TimeSpan.FromSeconds(30));

                Assert.Equal(1, subscriptions.GetAllMessages().Length);
            }
        }


        [Fact]
        public void Adding_subscription_that_already_exists_has_no_op_in_memory()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                var storage = container.Resolve<ISubscriptionStorage>();
                var wait = new ManualResetEvent(false);
                int count = 0;
                storage.SubscriptionChanged += () =>
                {
                    if (Interlocked.Increment(ref count) == 2)
                        wait.Set();
                };

                bus.Subscribe<MsmqSubscriptionTests.TestMessage>();
                bus.Subscribe<MsmqSubscriptionTests.TestMessage>();

                wait.WaitOne(TimeSpan.FromSeconds(30));

                var uris = storage.GetSubscriptionsFor(typeof(MsmqSubscriptionTests.TestMessage));
                Assert.Equal(1, uris.Count());
            }
        }

        [Fact]
        public void Adding_then_removing_will_result_in_compacted_queue()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                var listener = (AbstractMsmqListener) container.Resolve<ITransport>();
                var storage = container.Resolve<ISubscriptionStorage>();
                var waitSubscriptions = new ManualResetEvent(false);
                var waitListener = new ManualResetEvent(false);
                int count = 0;
                listener.MessageMoved += () => waitListener.Set();
                storage.SubscriptionChanged += () =>
                {
                    if (Interlocked.Increment(ref count) == 2)
                        waitSubscriptions.Set();
                };

                bus.Subscribe<MsmqSubscriptionTests.TestMessage>();
                bus.Subscribe<OnBusStart.TestMessage>();

                waitListener.WaitOne(TimeSpan.FromSeconds(30));
                waitSubscriptions.WaitOne(TimeSpan.FromSeconds(30));

                storage.RemoveSubscription(typeof(MsmqSubscriptionTests.TestMessage).FullName, TestQueueUri.Uri.ToString());

                count = 0;
                var enumerator2 = subscriptions.GetMessageEnumerator2();
                while (enumerator2.MoveNext(TimeSpan.FromSeconds(1)))
                    count += 1;
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void Can_add_subscription_to_queue()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
                var storage = container.Resolve<ISubscriptionStorage>();
                var wait = new ManualResetEvent(false);
                storage.SubscriptionChanged += () =>
                {
                    wait.Set();
                };

                bus.Subscribe<OnBusStart.TestMessage>();

                wait.WaitOne(TimeSpan.FromSeconds(30));

                var peek = subscriptions.Peek(TimeSpan.FromSeconds(30));

                Assert.NotNull(peek);
            }
        }



        [Fact]
        public void Can_remove_subscription_from_queue()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                var storage = container.Resolve<ISubscriptionStorage>();
                var wait = new ManualResetEvent(false);
                int count = 0;
                storage.SubscriptionChanged += () =>
                {
                    if (Interlocked.Increment(ref count) == 2)
                        wait.Set();
                };

                bus.Subscribe<MsmqSubscriptionTests.TestMessage>();
                subscriptions.Peek(TimeSpan.FromSeconds(30));
                bus.Unsubscribe<MsmqSubscriptionTests.TestMessage>();
                
                wait.WaitOne(TimeSpan.FromSeconds(30));
                try
                {
                    subscriptions.Peek(TimeSpan.FromSeconds(0));
                    Assert.False(true);
                }
                catch (MessageQueueException e)
                {
                    if (e.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                        throw;
                }
            }
        }
    }
}