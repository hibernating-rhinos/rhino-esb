using System.Messaging;
using Castle.Windsor;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class When_queue_does_not_exists : MsmqTestBase
    {
        private readonly IWindsorContainer container;

        public When_queue_does_not_exists()
        {
            container = new WindsorContainer();
            new RhinoServiceBusFacility()
                .UseCastleWindsor(container)
                .UseStandaloneConfigurationFile("BusWithLogging.config")
                .Configure();
        }


        [Fact]
        public void Will_automatically_create_it()
        {
            MessageQueue.Delete(testQueuePath);

            Assert.False(MessageQueue.Exists(testQueuePath));

            using(var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
            }

            Assert.True(MessageQueue.Exists(testQueuePath));
        }

        public override void Dispose()
        {
            base.Dispose();
            MessageQueue.Delete(testQueuePath);
        }
    }
}