using System;
using System.Linq;
using System.Messaging;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests.LoadBalancer
{
    public class With_accepting_work_queue : LoadBalancingTestBase
    {
        private readonly IWindsorContainer container;

        private const string acceptingWorkQueue = "msmq://localhost/test_queue.acceptingWork";
        private readonly string acceptingWorkQueuePath = MsmqUtil.GetQueuePath(new Uri(acceptingWorkQueue).ToEndpoint()).QueuePath;

        public With_accepting_work_queue()
        {
            if (MessageQueue.Exists(acceptingWorkQueuePath) == false)
                MessageQueue.Create(acceptingWorkQueuePath);
            var acceptingWork = new MessageQueue(acceptingWorkQueuePath);
            acceptingWork.Purge();

            container = new WindsorContainer();
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .UseStandaloneConfigurationFile(@"LoadBalancer\BusWithAcceptingWorkLoadBalancer.config")
                .Configure();

            container.Register(Component.For<MyHandler>());

            container.Register(
                Component.For<MsmqLoadBalancer>()
                    .DependsOn(new
                    {
                        threadCount = 1,
                        endpoint = new Uri(loadBalancerQueue),
                        transactional = TransactionalOptions.FigureItOut,
                        secondaryLoadBalancer = TestQueueUri2.Uri
                    })
                );

            container.Register(
               Component.For<MsmqReadyForWorkListener>()
                   .DependsOn(new
                   {
                       threadCount = 1,
                       endpoint = new Uri(acceptingWorkQueue),
                       transactional = TransactionalOptions.FigureItOut
                   })
               );

            container.Register(
               Component.For<MsmqSecondaryLoadBalancer>()
                   .DependsOn(new
                   {
                       threadCount = 1,
                       endpoint = TestQueueUri2.Uri,
                       primaryLoadBalancer = new Uri(loadBalancerQueue),
                       transactional = TransactionalOptions.FigureItOut
                   })
               );
        }

        [Fact]
        public void Can_send_message_through_load_balancer()
        {
            MyHandler.ResetEvent = new ManualResetEvent(false);

            using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                loadBalancer.Start();
                bus.Start();

                bus.Send(loadBalancer.Endpoint, "abcdefg");

                MyHandler.ResetEvent.WaitOne(TimeSpan.FromSeconds(30), false);
                Assert.True(
                        MyHandler.Message.ResponseQueue.Path.Contains(@"private$\test_queue")
                        );

                Assert.Equal("abcdefg", MyHandler.Value);

            }
        }

        [Fact]
        public void When_worker_tell_load_balancer_that_it_is_ready_the_worker_will_be_added_to_known_queues()
        {
            using (var loadBalancer = new MessageQueue(loadBalancerQueuePath + ";Workers", QueueAccessMode.SendAndReceive))
            {
                loadBalancer.Purge();
            }

            using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                loadBalancer.Start();
                bus.Start();

                using (var workers = new MessageQueue(loadBalancerQueuePath + ";Workers", QueueAccessMode.SendAndReceive))
                {
                    workers.Formatter = new XmlMessageFormatter(new[] { typeof(string) });
                    var knownWorker = workers.Peek(TimeSpan.FromSeconds(30));
                    Assert.Equal(bus.Endpoint.Uri.ToString(), knownWorker.Body.ToString());
                }

                Assert.True(loadBalancer.KnownWorkers.GetValues().Contains(TestQueueUri.Uri));
            }
        }

        [Fact]
        public void when_start_load_balancer_that_has_secondary_will_send_reroute_to_ready_for_work_queue_to_workers_to_relieve_secondary()
        {
            using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
            {
                loadBalancer.KnownWorkers.Add(TestQueueUri.Uri);
                loadBalancer.Start();

                var message = queue.Receive(TimeSpan.FromSeconds(5));
                var serializer = container.Resolve<IMessageSerializer>();
                var reroute = serializer.Deserialize(message.BodyStream)
                    .OfType<Reroute>().First();

                Assert.Equal(loadBalancer.ReadyForWorkListener.Endpoint.Uri, reroute.NewEndPoint);
                Assert.Equal(loadBalancer.ReadyForWorkListener.Endpoint.Uri, reroute.OriginalEndPoint);
            }
        }

        [Fact]
        public void When_secondary_starts_it_will_ask_primary_to_get_ready_to_work_uri()
        {
            using (var secondary = container.Resolve<MsmqSecondaryLoadBalancer>())
            {
                secondary.Start();

                using (var loadBalancerMsmqQueue = new MessageQueue(loadBalancerQueuePath))
                {
                    int tries = 5;
                    QueryReadyForWorkQueueUri query = null;
                    var serializer = container.Resolve<IMessageSerializer>();
                    while (query == null)
                    {
                        var message = loadBalancerMsmqQueue.Receive(TimeSpan.FromSeconds(30));
                        query = serializer.Deserialize(message.BodyStream)
                            .OfType<QueryReadyForWorkQueueUri>().FirstOrDefault();
                        Assert.True(tries > 0);
                        tries -= 1;
                    }

                    Assert.NotNull(query);
                }
            }
        }

        [Fact]
        public void When_secondary_takes_over_it_will_let_workers_know_that_it_took_over_and_reroute_to_ready_to_work_queue()
        {
            using (var primary = container.Resolve<MsmqLoadBalancer>())
            using (var secondary = container.Resolve<MsmqSecondaryLoadBalancer>())
            {
                primary.Start();
                secondary.TimeoutForHeartBeatFromPrimary = TimeSpan.FromMilliseconds(900);
                secondary.KnownWorkers.Add(TestQueueUri.Uri);
                secondary.KnownEndpoints.Add(TestQueueUri.Uri);//any worker is also endpoint

                var wait = new ManualResetEvent(false);
                secondary.TookOverAsActiveLoadBalancer += () => wait.Set();
                secondary.Start();

                Assert.True(wait.WaitOne());

                int tries = 5;
                var serializer = container.Resolve<IMessageSerializer>();
                Reroute reroute = null;
                while (reroute == null)
                {
                    var message = queue.Receive(TimeSpan.FromSeconds(30));
                    reroute = serializer.Deserialize(message.BodyStream)
                        .OfType<Reroute>().FirstOrDefault();
                    Assert.True(tries > 0);
                    tries -= 1;
                }

                Assert.Equal(secondary.PrimaryLoadBalancer, reroute.OriginalEndPoint);
                Assert.Equal(secondary.Endpoint.Uri, reroute.NewEndPoint);

                reroute = null;
                while (reroute == null)
                {
                    var message = queue.Receive(TimeSpan.FromSeconds(30));
                    reroute = serializer.Deserialize(message.BodyStream)
                        .OfType<Reroute>().FirstOrDefault();
                    Assert.True(tries > 0);
                    tries -= 1;
                }

                Assert.Equal(primary.ReadyForWorkListener.Endpoint.Uri, reroute.OriginalEndPoint);
                Assert.Equal(secondary.ReadyForWorkListener.Endpoint.Uri, reroute.NewEndPoint);
            }
        }


        public class MyHandler : ConsumerOf<string>
        {
            public static ManualResetEvent ResetEvent;
            public static string Value;
            public static Message Message;

            public void Consume(string message)
            {
                Message = MsmqTransport.MsmqCurrentMessageInformation.MsmqMessage;
                Value = message;
                ResetEvent.Set();
            }

        }

    }
}
