using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
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
            container = new WindsorContainer();
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .Configure();
            container.Register(Component.For<StringConsumer>().LifeStyle.Transient);
            StringConsumer.Value = null;
            StringConsumer.Event = new ManualResetEvent(false);
        }

        [Fact]
        public void SendMessageToRemoteBus()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                var oneWay = new MsmqOnewayBus(new[]
                {
                    new MessageOwner
                    {
                        Endpoint = bus.Endpoint.Uri,
                        Name = "System",
                    },
                }, new MsmqMessageBuilder(container.Resolve<IMessageSerializer>(), container.Resolve<IServiceLocator>()));

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

                using (var c = new WindsorContainer())
                {
                    new OnewayRhinoServiceBusConfiguration()
                        .UseCastleWindsor(c)
                        .UseStandaloneConfigurationFile("OneWayBus.config")
                        .Configure();
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