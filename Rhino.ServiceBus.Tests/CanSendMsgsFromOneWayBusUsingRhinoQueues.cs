using System;
using System.IO;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.Queues;
using Rhino.ServiceBus.Hosting;
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
            if (Directory.Exists("one_way2.esent"))
                Directory.Delete("one_way2.esent", true);
            if (Directory.Exists("test_queue.esent"))
                Directory.Delete("test_queue.esent", true);
            if (Directory.Exists("test_queue_subscriptions.esent"))
                Directory.Delete("test_queue_subscriptions.esent", true);
            container = new WindsorContainer();
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .UseStandaloneConfigurationFile("ReceiveOneWayBusRhinoQueues.config")
                .Configure();
            container.Register(Component.For<StringConsumer>());
            container.Register(Component.For<IntConsumer>());
            StringConsumer.Value = null;
            StringConsumer.Event = new ManualResetEvent(false);

            IntConsumer.Count = 0;
            IntConsumer.Total = 0;
            IntConsumer.Event = new ManualResetEvent(false);
        }



        [Fact]
        public void SendMessageToRemoteBus()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                using (var oneWay = new RhinoQueuesOneWayBus(
                    new[]{
                            new MessageOwner
                                {
                                    Endpoint = bus.Endpoint.Uri,
                                    Name = "System",
                                },
                        }, 
                        container.Resolve<IMessageSerializer>(),
                        Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), "one_way.esent"),
                        false,
                        new RhinoQueuesMessageBuilder(container.Resolve<IMessageSerializer>(),
                            container.Resolve<IServiceLocator>()),
                        new QueueManagerConfiguration()))
                {
                    oneWay.Send("hello there, one way");

                    StringConsumer.Event.WaitOne(TimeSpan.FromSeconds(3));
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
                    new OnewayRhinoServiceBusConfiguration()
                        .UseCastleWindsor(c)
                        .UseStandaloneConfigurationFile("OneWayBusRhinoQueues.config")
                        .Configure();
                    var oneway = c.Resolve<IOnewayBus>();
                    oneway.Send("hello there, one way");
                    StringConsumer.Event.WaitOne(TimeSpan.FromSeconds(3));
                    Assert.Equal("hello there, one way", StringConsumer.Value);
                }

                
            }
        }

        [Fact]
        public void CanRunTwoOneWayBusesOnSameMachine()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                var config1 = new HostConfiguration()
                    .AddAssembly(typeof(RhinoQueuesTransport).Assembly)
                    .Receive("System.Int", "rhino.queues://localhost/test_queue");
                
                //Must specify an alternate storage path since we are running both out of same base directory.
                var config2 = new HostConfiguration()
                    .AddAssembly(typeof(RhinoQueuesTransport).Assembly)
                    .StoragePath(Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), "one_way2.esent"))
                    .Receive("System.Int", "rhino.queues://localhost/test_queue");
                

                using (var c1 = new WindsorContainer())
                using (var c2 = new WindsorContainer())
                {
                    new OnewayRhinoServiceBusConfiguration()
                        .UseCastleWindsor(c1)
                        .UseConfiguration(config1.ToBusConfiguration())
                        .Configure();

                    new OnewayRhinoServiceBusConfiguration()
                        .UseCastleWindsor(c2)
                        .UseConfiguration(config2.ToBusConfiguration())
                        .Configure();

                    var onewayBus1 = c1.Resolve<IOnewayBus>();
                    var onewayBus2 = c2.Resolve<IOnewayBus>();

                    onewayBus1.Send(1);
                    onewayBus2.Send(2);

                    IntConsumer.Event.WaitOne(TimeSpan.FromSeconds(3));
                    Assert.Equal(2, IntConsumer.Count);
                    Assert.Equal(3, IntConsumer.Total);
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

        public class IntConsumer : ConsumerOf<int>
        {
            public static ManualResetEvent Event;
            public static int Total;
            public static int Count;
            public void Consume(int pong)
            {
                Total += pong;
                Count++;
                if(Count == 2) Event.Set();
            }
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}