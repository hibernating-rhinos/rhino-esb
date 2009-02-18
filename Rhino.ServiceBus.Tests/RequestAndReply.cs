using System;
using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class RequestAndReply : MsmqTestBase,
        ConsumerOf<RequestAndReply.PongMessage>
    {
        private readonly WindsorContainer container;
        private readonly ManualResetEvent handle = new ManualResetEvent(false);
        private PongMessage message;

        public RequestAndReply()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            container.AddComponent<PingConsumer>();
        }


        [Fact]
        public void Can_request_and_reply()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                using (bus.AddInstanceSubscription(this))
                {
                    bus.Send(bus.Endpoint, new PingMessage());

                    handle.WaitOne(TimeSpan.FromSeconds(30), false);

                    Assert.NotNull(message);
                }
            }

        }

        [Fact]
        public void Bus_will_not_hold_reference_to_consumer()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                var weakConsumer = new WeakReference(new PingConsumer(bus));

                using (bus.AddInstanceSubscription((IMessageConsumer)weakConsumer.Target))
                {
                  
                }
                GC.Collect(2);
                GC.WaitForPendingFinalizers();
             
                Assert.False(weakConsumer.IsAlive);
            }
        }

        [Fact]
        public void Can_request_and_reply_and_then_unsubscribe()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                using (bus.AddInstanceSubscription(this))
                {
                    bus.Send(bus.Endpoint, new PingMessage());

                    handle.WaitOne(TimeSpan.FromSeconds(30), false);

                    Assert.NotNull(message);
                }

                handle.Reset();

                message = null;
                container.Resolve<ITransport>().MessageArrived += m => handle.Set();
                bus.Send(bus.Endpoint, new PingMessage());

                handle.WaitOne(TimeSpan.FromSeconds(30), false);

                Assert.Null(message);
            }
        }

        public class PingMessage { }
        public class PongMessage { }

        public class PingConsumer : ConsumerOf<PingMessage>
        {
            private readonly IServiceBus bus;

            public PingConsumer(IServiceBus bus)
            {
                this.bus = bus;
            }

            public void Consume(PingMessage pong)
            {
                bus.Reply(new PongMessage());
            }
        }

        public void Consume(PongMessage pong)
        {
            message = pong;
            handle.Set();
        }
    }
}