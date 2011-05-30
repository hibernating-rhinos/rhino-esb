using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;

namespace Rhino.ServiceBus.Tests
{
    using System;
    using System.Threading;
    using Impl;
    using Xunit;

    public class DelayedMessages : MsmqTestBase
    {
        private readonly WindsorContainer container;

        public DelayedMessages()
        {
            container = new WindsorContainer(new XmlInterpreter());
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .Configure();
            container.Register(
                Component.For<HandleMessageLater>(),
                Component.For<ProcessInteger>()
                );
        }

        [Fact]
        public void Can_handle_message_later()
        {
            HandleMessageLater.ResetEvent = new ManualResetEvent(false);
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
                bus.Send(bus.Endpoint, "foobar");

                HandleMessageLater.ResetEvent.WaitOne(TimeSpan.FromSeconds(30), false);

                Assert.True(HandleMessageLater.recievedFirst);
                Assert.True(HandleMessageLater.recievedSecond);
            }
        }

        [Fact]
        public void Can_send_message_with_time_delay()
        {
            HandleMessageLater.ResetEvent = new ManualResetEvent(false);
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                ProcessInteger.Timestamp = DateTime.MinValue;
                ProcessInteger.ResetEvent = new ManualResetEvent(false);

                var beforeSend = DateTime.Now;
                bus.DelaySend(bus.Endpoint, DateTime.Now.AddMilliseconds(250), 5);
                Assert.True(ProcessInteger.ResetEvent.WaitOne(TimeSpan.FromSeconds(30), false));

                Assert.True((DateTime.Now - beforeSend).TotalMilliseconds >= 250);
            }
        }

        public class ProcessInteger : ConsumerOf<int>
        {
            public static DateTime Timestamp;
            public static ManualResetEvent ResetEvent;

            public void Consume(int message)
            {
                Timestamp = DateTime.Now;
                ResetEvent.Set();
            }
        }

        public class HandleMessageLater : ConsumerOf<string>
        {
            public static bool recievedFirst;
            public static bool recievedSecond;
            public static ManualResetEvent ResetEvent;
            private readonly IServiceBus bus;

            public HandleMessageLater(IServiceBus bus)
            {
                this.bus = bus;
            }

            public void Consume(string message)
            {
                if (recievedFirst == false)
                {
                    recievedFirst = true;
                    bus.HandleCurrentMessageLater();
                }
                else
                {
                    recievedSecond = true;
                    ResetEvent.Set();
                }
            }
        }
    }
}