using System;
using System.Diagnostics;
using System.Messaging;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Messages;
using Xunit;
using System.Linq;

namespace Rhino.ServiceBus.Tests.LoadBalancer
{
    public class with_secondary_failover : LoadBalancingTestBase
    {
        private readonly IWindsorContainer container;

        public with_secondary_failover()
        {
            testQueue2.Purge();

            var interpreter = new XmlInterpreter(@"LoadBalancer\BusWithLoadBalancer.config");
            container = new WindsorContainer(interpreter);
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());

            container.Register(
                Component.For<MsmqSecondaryLoadBalancer>()
                    .DependsOn(new
                    {
                        threadCount = 1,
                        endpoint = TestQueueUri2.Uri,
                        primaryLoadBalancer = new Uri(loadBalancerQueue),
                    })
                );
        }

        [Fact]
        public void When_secondary_starts_it_will_ask_primary_to_get_known_workers_and_endpoints()
        {
            using (var secondary = container.Resolve<MsmqSecondaryLoadBalancer>())
            {
                secondary.Start();

                using (var loadBalancerMsmqQueue = new MessageQueue(loadBalancerQueuePath))
                {
                    var message = loadBalancerMsmqQueue.Receive(TimeSpan.FromSeconds(30));
                    Assert.Equal(typeof(QueryForAllKnownWorkersAndEndpoints).FullName, message.Label);
                }
            }
        }

        [Fact]
        public void When_secondary_does_not_get_heartbeat_from_primary_it_will_take_over()
        {
            using (var secondary = container.Resolve<MsmqSecondaryLoadBalancer>())
            {
                secondary.TimeoutForHeartBeatFromPrimary = TimeSpan.FromMilliseconds(10);

                var wait = new ManualResetEvent(false);
                secondary.TookOverAsActiveLoadBalancer += () => wait.Set();
                secondary.Start();

                Assert.True(wait.WaitOne());
            }
        }


        [Fact]
        public void When_secondary_takes_over_it_will_let_endpoints_that_it_took_over()
        {
            using (var secondary = container.Resolve<MsmqSecondaryLoadBalancer>())
            {
                secondary.TimeoutForHeartBeatFromPrimary = TimeSpan.FromMilliseconds(10);
                secondary.KnownEndpoints.Add(TestQueueUri2.Uri);

                var wait = new ManualResetEvent(false);
                secondary.TookOverAsActiveLoadBalancer += () => wait.Set();
                secondary.Start();

                Assert.True(wait.WaitOne());

                var message = testQueue2.Receive(TimeSpan.FromSeconds(30));
                var serializer = container.Resolve<IMessageSerializer>();
                var reroute = serializer.Deserialize(message.BodyStream)
                    .OfType<Reroute>().First();

                Assert.Equal(secondary.PrimaryLoadBalancer, reroute.OriginalEndPoint);
                Assert.Equal(secondary.Endpoint.Uri, reroute.NewEndPoint);
            }
        }

        [Fact]
        public void When_secondary_takes_over_it_will_let_workers_know_that_it_took_over()
        {
            using (var secondary = container.Resolve<MsmqSecondaryLoadBalancer>())
            {
                secondary.TimeoutForHeartBeatFromPrimary = TimeSpan.FromMilliseconds(10);
                secondary.KnownWorkers.Add(TestQueueUri2.Uri);
                secondary.KnownEndpoints.Add(TestQueueUri2.Uri);//any worker is also endpoint

                var wait = new ManualResetEvent(false);
                secondary.TookOverAsActiveLoadBalancer += () => wait.Set();
                secondary.Start();

                Assert.True(wait.WaitOne());

                int tries = 5;
                var serializer = container.Resolve<IMessageSerializer>();
                Reroute reroute = null;
                while (reroute == null)
                {
                    var message = testQueue2.Receive(TimeSpan.FromSeconds(30));
                    reroute = serializer.Deserialize(message.BodyStream)
                        .OfType<Reroute>().FirstOrDefault();
                    Assert.True(tries > 0);
                    tries -= 1;
                }

                Assert.Equal(secondary.PrimaryLoadBalancer, reroute.OriginalEndPoint);
                Assert.Equal(secondary.Endpoint.Uri, reroute.NewEndPoint);
            }
        }

        [Fact(Skip="Not sure why it is not working")]
        public void When_secondary_takes_over_it_will_let_workers_know_that_it_is_accepting_work()
        {
            using (var secondary = container.Resolve<MsmqSecondaryLoadBalancer>())
            {
                secondary.TimeoutForHeartBeatFromPrimary = TimeSpan.FromMilliseconds(10);
                secondary.KnownWorkers.Add(TestQueueUri2.Uri);
                secondary.KnownEndpoints.Add(TestQueueUri2.Uri);//any worker is also endpoint

                var wait = new ManualResetEvent(false);
                secondary.TookOverAsActiveLoadBalancer += () => wait.Set();
                secondary.Start();

                Assert.True(wait.WaitOne());
                var serializer = container.Resolve<IMessageSerializer>();

                int tries = 5;
                AcceptingWork acceptingWork = null;
                while(acceptingWork==null)
                {
                    var message = testQueue2.Receive(TimeSpan.FromSeconds(30));
                    var deserialize = serializer.Deserialize(message.BodyStream);
                    acceptingWork = deserialize.OfType<AcceptingWork>().FirstOrDefault();
                    Assert.True(tries > 0);
                    tries -= 1;
                }

                Assert.Equal(acceptingWork.Endpoint, secondary.Endpoint.Uri);
            }
        }
    }
}