using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class PublishingTests : MsmqTestBase
    {
        private readonly IWindsorContainer container;

        public PublishingTests()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
        }

        [Fact]
        public void Trying_to_publish_with_no_notifications_will_throw()
        {
            using(var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                Assert.Throws<MessagePublicationException>(() => bus.Publish("test"));
            }
        }

        [Fact]
        public void Trying_to_notify_with_no_notifications_will_throw()
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
    }
}