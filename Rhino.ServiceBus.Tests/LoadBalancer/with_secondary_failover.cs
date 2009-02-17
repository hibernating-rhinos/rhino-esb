using System;
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
            using(var secondary = container.Resolve<MsmqSecondaryLoadBalancer>())
            {
                secondary.Start();

                using(var loadBalancerMsmqQueue = new MessageQueue(loadBalancerQueuePath))
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


        [Fact(Skip = "Need additional review, could not get them to consistently run")]
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

		[Fact(Skip = "Need additional review, could not get them to consistently run")]
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

                var message = testQueue2.Receive(TimeSpan.FromSeconds(30));
                var serializer = container.Resolve<IMessageSerializer>();
				Reroute reroute = serializer.Deserialize(message.BodyStream)
            			.OfType<Reroute>().First();

                Assert.Equal(secondary.PrimaryLoadBalancer, reroute.OriginalEndPoint);
                Assert.Equal(secondary.Endpoint.Uri.ToString().ToLower(), 
                    reroute.NewEndPoint.ToString().ToLower().Replace(Environment.MachineName.ToLower(),"localhost"));
            }
        }

		[Fact(Skip = "Need additional review, could not get them to consistently run")]
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

                testQueue2.Receive(TimeSpan.FromSeconds(30));//reroute message
                var message = testQueue2.Receive(TimeSpan.FromSeconds(30));
                var serializer = container.Resolve<IMessageSerializer>();
                var deserialize = serializer.Deserialize(message.BodyStream);
                var acceptingWork = deserialize.OfType<AcceptingWork>().First();

                Assert.Equal(acceptingWork.Endpoint, secondary.Endpoint.Uri);
            }
        }
    }
}