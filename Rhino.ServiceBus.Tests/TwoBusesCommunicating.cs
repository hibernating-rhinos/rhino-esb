using System;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class TwoBusesCommunicating : MsmqTestBase
    {
        private readonly IWindsorContainer container1;
        private readonly IWindsorContainer container2;

        public TwoBusesCommunicating()
        {
            container1 = new WindsorContainer(new XmlInterpreter());
            container2 = new WindsorContainer();

            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container1)
                .Configure();

            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container2)
                .UseStandaloneConfigurationFile("AnotherBus.config")
                .Configure();

            container1.Register(Component.For<PingHandler>());
            container2.Register(Component.For<PongHandler>());
        }

        [Fact]
        public void Can_send_messages_from_one_end_to_the_other()
        {
            using(var bus1 = container1.Resolve<IStartableServiceBus>())
            using(var bus2 = container2.Resolve<IStartableServiceBus>())
            {
                var subscriptionStorage2 = container2.Resolve<ISubscriptionStorage>();
                
                var wait = new ManualResetEvent(false);
                subscriptionStorage2.SubscriptionChanged += () => wait.Set();
                
                bus1.Start();
                bus2.Start();

                PongHandler.ResetEvent = new ManualResetEvent(false);
                PongHandler.GotReply = false;

                wait.WaitOne(TimeSpan.FromSeconds(30), false);

                bus2.Publish(new Ping());

                PongHandler.ResetEvent.WaitOne(TimeSpan.FromSeconds(30), false);

                Assert.True(PongHandler.GotReply);
            }
        }
        [Fact]
        public void Can_notify_when_at_least_one_message_has_handler()
        {
            using(var bus1 = container1.Resolve<IStartableServiceBus>())
            using(var bus2 = container2.Resolve<IStartableServiceBus>())
            {
                var subscriptionStorage2 = container2.Resolve<ISubscriptionStorage>();
                
                var wait = new ManualResetEvent(false);
                subscriptionStorage2.SubscriptionChanged += () => wait.Set();
                
                bus1.Start();
                bus2.Start();

                PongHandler.ResetEvent = new ManualResetEvent(false);
                PongHandler.GotReply = false;

                wait.WaitOne(TimeSpan.FromSeconds(30), false);

                bus2.Notify("ping", new Ping());

                PongHandler.ResetEvent.WaitOne(TimeSpan.FromSeconds(30), false);

                Assert.True(PongHandler.GotReply);
            }
        }

        public class Ping
        {
            
        }

        public class Pong
        {
            
        }

        public class PingHandler : ConsumerOf<Ping>
        {
            private readonly IServiceBus bus;

            public PingHandler(IServiceBus bus)
            {
                this.bus = bus;
            }

            public void Consume(Ping message)
            {
                bus.Reply(new Pong());    
            }
        }

        public class PongHandler : ConsumerOf<Pong>
        {
            public static ManualResetEvent ResetEvent;
            public static bool GotReply;

            public void Consume(Pong message)
            {
                GotReply = true;
                ResetEvent.Set();
            }
        }
    }
}