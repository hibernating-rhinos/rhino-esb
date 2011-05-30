using System;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class PublishingTests : MsmqTestBase
    {
        private readonly IWindsorContainer container;

        public PublishingTests()
        {
            container = new WindsorContainer(new XmlInterpreter());
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .Configure();
        }

        [Fact]
        public void Can_publish_to_consumers_of_interface()
        {
            container.Register(Component.For<TestMessageConsumer>()
                .LifeStyle.Transient);
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
                var storage = container.Resolve<ISubscriptionStorage>();
                var wait = new ManualResetEvent(false);
                storage.SubscriptionChanged += () =>
                {
                    wait.Set();
                };

                wait.WaitOne(TimeSpan.FromSeconds(5));
                Assert.DoesNotThrow(() => bus.Publish(new TestMessage { Id = 1 }));
            }
        }

        [Fact]
        public void Trying_to_publish_with_no_notifications_will_throw()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                Assert.Throws<MessagePublicationException>(() => bus.Publish("test"));
            }
        }

        [Fact]
        public void Trying_to_notify_with_no_notifications_will_not_throw()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                Assert.DoesNotThrow(() => bus.Notify("test"));
            }
        }

        [Fact]
        public void Trying_to_send_with_no_owner_will_throw()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                Assert.Throws<MessagePublicationException>(() => bus.Send("test"));
            }
        }

        public interface ITestMessage
        {
            int Id { get; set; }
        }

        public class TestMessage : ITestMessage
        {
            public int Id { get; set; }
        }

        public class TestMessageConsumer : ConsumerOf<ITestMessage>
        {
            public void Consume(ITestMessage message)
            {
                
            }
        }
    }
}