using System;
using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
	public class RequestReplyWorksWithNoAutomaticSubscriptionConsumer : 
		OccasionalConsumerOf<RequestReplyWorksWithNoAutomaticSubscriptionConsumer.PongMessage>
	{
		private readonly WindsorContainer container;
        private readonly ManualResetEvent handle = new ManualResetEvent(false);
        private PongMessage message;

		public RequestReplyWorksWithNoAutomaticSubscriptionConsumer()
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

        public class PingMessage { }
        public class PongMessage { }

        public class PingConsumer : Consumer<PingMessage>.SkipAutomaticSubscription
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