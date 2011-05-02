using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

// ReSharper disable InconsistentNaming

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class When_subscribing_instance_subscriptions : MsmqTestBase
    {
        private static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer(new XmlInterpreter());
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .Configure();
            return container;
        }

        [Fact]
        public void Should_send_AddInstanceSubscriptionMessage_on_subscribe()
        {
            var container = CreateContainer();
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
                var consumer = new TestConsumer();

                // purposefully not calling dispose on the result of this method
                bus.AddInstanceSubscription(consumer);
            }

            var messages = testQueue2.GetAllMessages();
            Assert.Equal(1, messages.Length);
            Assert.Equal("Rhino.ServiceBus.Messages.AddInstanceSubscription", messages[0].Label);
        }

        [Fact]
        public void Should_send_RemoveInstanceSubscriptionMessage_on_unsubscribe()
        {
            var container = CreateContainer();
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
                var consumer = new TestConsumer();
                using (bus.AddInstanceSubscription(consumer))
                {
                    
                }
            }

            var messages = testQueue2.GetAllMessages();
            Assert.Equal(2, messages.Length);
            Assert.Equal("Rhino.ServiceBus.Messages.AddInstanceSubscription", messages[0].Label);
            Assert.Equal("Rhino.ServiceBus.Messages.RemoveInstanceSubscription", messages[1].Label);
        }

        public class TestMessage
        {
            
        }

        public class TestConsumer : OccasionalConsumerOf<TestMessage>
        {
            public void Consume(TestMessage message)
            {
                // Does nothing
            }
        }
    }
}
