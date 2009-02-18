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

namespace Rhino.ServiceBus.Tests
{
    public class OnBusStart : MsmqTestBase
    {
        private readonly IWindsorContainer container;

        public OnBusStart()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            container.AddComponent<TestHandler>();
            container.AddComponent<OccasionalTestHandler>();
        }

        [Fact]
        public void Should_subscribe_to_all_handlers_automatically()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                var wait = new ManualResetEvent(false);
                var subscriptionStorage = container.Resolve<ISubscriptionStorage>();
                
                subscriptionStorage.SubscriptionChanged += () => wait.Set();
              
                bus.Start();
                
                wait.WaitOne(TimeSpan.FromSeconds(30), false);

                var serializer = container.Resolve<IMessageSerializer>();
                bool found = false;
                subscriptions.Peek(TimeSpan.FromSeconds(30));
                var messagesEnum = subscriptions.GetMessageEnumerator2();
                while (messagesEnum.MoveNext(TimeSpan.FromSeconds(0)))
                {
                    var message = messagesEnum.Current;
                    var subscription = (AddSubscription)serializer.Deserialize(message.BodyStream)[0];
                    if(subscription.Type==typeof(TestMessage).FullName)
                    {
                        found = true;
                        break;
                    }
                }
                Assert.True(found);
            }
        }

        [Fact]
        public void Would_not_automatically_subscribe_occasional_consumers()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                var wait = new ManualResetEvent(false);
                var subscriptionStorage = container.Resolve<ISubscriptionStorage>();

                subscriptionStorage.SubscriptionChanged += () => wait.Set();
                bus.Start();
                wait.WaitOne(TimeSpan.FromSeconds(30), false);

                var serializer = container.Resolve<IMessageSerializer>();
                subscriptions.Peek(TimeSpan.FromSeconds(30));
                var messages = subscriptions.GetAllMessages();
                foreach (var message in messages)
                {
                    var subscription = (AddSubscription)serializer.Deserialize(message.BodyStream)[0];
                    Assert.NotEqual(typeof (OccasionalTestHandler).FullName, subscription.Type);
                }
            }
        }

        public class TestMessage { }
        public class AnotherTestMessage { }
        public class TestHandler : ConsumerOf<TestMessage>
        {
            public void Consume(TestMessage message)
            {
            }
        }

        public class OccasionalTestHandler : OccasionalConsumerOf<AnotherTestMessage>
        {
            public void Consume(AnotherTestMessage message)
            {
            }
        }
    }
}