using System;
using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class SendByMessageOwner : MsmqTestBase
    {

        private readonly IWindsorContainer container;

        public SendByMessageOwner()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            container.AddComponent<TestHandler>();
        }


        [Fact]
        public void Send_by_endpoint_in_config()
        {
            using(var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
                TestHandler.ResetEvent = new ManualResetEvent(false);
                TestHandler.GotMessage = false;

                bus.Send(new TestMessage());

                TestHandler.ResetEvent.WaitOne(TimeSpan.FromSeconds(30), false);

                Assert.True(TestHandler.GotMessage);
            }
        }

        public class TestMessage{}

        public class TestHandler : ConsumerOf<TestMessage>
        {
            public static bool GotMessage;
            public static ManualResetEvent ResetEvent;

            public void Consume(TestMessage message)
            {
                GotMessage = true;
                ResetEvent.Set();
            }
        }
    }
}