using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class CanSendMsgsFromOneWayBus : MsmqTestBase
    {
        private readonly WindsorContainer container;

        public CanSendMsgsFromOneWayBus()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            container.AddComponent<StringConsumer>();
            StringConsumer.Value = null;
            StringConsumer.Event = new ManualResetEvent(false);
        }

        [Fact]
        public void SendMessageToRemoteBus()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                var oneWay = new OnewayBus(new[]
                {
                    new MessageOwner
                    {
                        Endpoint = bus.Endpoint.Uri,
                        Name = "System",
                    },
                }, new MessageBuilder(container.Resolve<IMessageSerializer>(), null));

                oneWay.Send("hello there, one way");

                StringConsumer.Event.WaitOne();

                Assert.Equal("hello there, one way", StringConsumer.Value);
            }

        }

        [Fact]
        public void SendMessageToRemoteBusFromConfigDrivenOneWayBus()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {           
                bus.Start();

                using(var c = new WindsorContainer(new XmlInterpreter("OneWayBus.config")))
                {
                    c.Kernel.AddFacility("one.way.rhino.esb", new OnewayRhinoServiceBusFacility());
                    c.Resolve<IOnewayBus>().Send("hello there, one way");
                }

                StringConsumer.Event.WaitOne();

                Assert.Equal("hello there, one way", StringConsumer.Value);
            }

        }

        public class StringConsumer : ConsumerOf<string>
        {
            public static ManualResetEvent Event;
            public static string Value;
            public void Consume(string pong)
            {
                Value = pong;
                Event.Set();
            }
        }
    }
}