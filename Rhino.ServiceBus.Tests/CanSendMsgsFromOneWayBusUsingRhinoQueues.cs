using System;
using System.IO;
using System.Threading;
using System.Transactions;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.RhinoQueues;
using Rhino.ServiceBus.Tests.RhinoQueues;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class CanSendMsgsFromOneWayBusUsingRhinoQueues : WithDebugging,IDisposable
    {
        private WindsorContainer container;

        public CanSendMsgsFromOneWayBusUsingRhinoQueues()
        {
            if (Directory.Exists("one_way.esent"))
                Directory.Delete("one_way.esent", true);
            if (Directory.Exists("test_queue.esent"))
                Directory.Delete("test_queue.esent", true);
            if (Directory.Exists("test_queue_subscriptions.esent"))
                Directory.Delete("test_queue_subscriptions.esent", true);
            container = new WindsorContainer();
            new RhinoServiceBusFacility()
                .UseCastleWindsor(container)
                .UseStandaloneConfigurationFile("ReceiveOneWayBusRhinoQueues.config")
                .Configure();
            container.Register(Component.For<StringConsumer>());
            StringConsumer.Value = null;
            StringConsumer.Event = new ManualResetEvent(false);
        }



        [Fact]
        public void SendMessageToRemoteBus()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                using (var oneWay = new RhinoQueuesOneWayBus(new[]
                                                 {
                                                     new MessageOwner
                                                         {
                                                             Endpoint = bus.Endpoint.Uri,
                                                             Name = "System",
                                                         },
                                                 }, container.Resolve<IMessageSerializer>(), new RhinoQueuesMessageBuilder(container.Resolve<IMessageSerializer>())))
                {
                    oneWay.Send("hello there, one way");

                    StringConsumer.Event.WaitOne();
                }

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
                    new OnewayRhinoServiceBusFacility()
                        .UseCastleWindsor(c)
                        .UseStandaloneConfigurationFile("OneWayBusRhinoQueues.config")
                        .Configure();
                    var oneway = c.Resolve<IOnewayBus>();
                    oneway.Send("hello there, one way");
                    StringConsumer.Event.WaitOne();
                    Assert.Equal("hello there, one way", StringConsumer.Value);
                }

                
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

        public void Dispose()
        {
            container.Dispose();
        }
    }
}