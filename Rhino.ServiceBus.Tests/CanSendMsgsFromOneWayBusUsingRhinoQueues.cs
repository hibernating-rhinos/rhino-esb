using System;
using System.IO;
using System.Threading;
using System.Transactions;
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
            container = new WindsorContainer(new XmlInterpreter("OneWayBusRhinoQueues.config"));
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
                var transport = new RhinoQueuesTransport(new Uri("null://nowhere:24689/middle"),
                                                         new EndpointRouter(), container.Resolve<IMessageSerializer>(),
                                                         1, "one_way.esent", IsolationLevel.ReadCommitted, 5);
                var oneWay = new RhinoQueuesOneWayBus(new[]
                                                 {
                                                     new MessageOwner
                                                         {
                                                             Endpoint = bus.Endpoint.Uri,
                                                             Name = "System",
                                                         },
                                                 }, transport);

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

                using (var c = new WindsorContainer(new XmlInterpreter("OneWayBusRhinoQueues.config")))
                {
                    c.Kernel.AddFacility("one.way.rhino.esb", new OnewayRhinoServiceBusFacility());
                    c.Resolve<IOnewayBus>().Send("hello there, one way");
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