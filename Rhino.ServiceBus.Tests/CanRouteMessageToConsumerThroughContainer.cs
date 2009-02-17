using System;
using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class CanRouteMessageToConsumerThroughContainer : MsmqTestBase
    {
        private readonly IWindsorContainer container;

        public CanRouteMessageToConsumerThroughContainer()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            container.AddComponent<TestConsumer>();
        }

        [Fact]
        public void A_message_will_be_routed_to_consumer()
        {
            using(var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
                
                var msg = DateTime.Now.Ticks;

                TestConsumer.Wait = new ManualResetEvent(false);

                var transport = container.Resolve<ITransport>();
                transport.Send(transport.Endpoint, msg);

                TestConsumer.Wait.WaitOne(TimeSpan.FromSeconds(30));

                Assert.Equal(msg, TestConsumer.Ticks);
            }
        }

        public class TestConsumer : ConsumerOf<long>
        {
            public static long Ticks;
            public static ManualResetEvent Wait;

            public void Consume(long pong)
            {
                Ticks = pong;
                Wait.Set();
            }
        }
    }
}