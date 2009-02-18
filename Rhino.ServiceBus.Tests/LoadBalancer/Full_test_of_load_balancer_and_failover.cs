using System;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Xunit;

namespace Rhino.ServiceBus.Tests.LoadBalancer
{
    public class Full_test_of_load_balancer_and_failover : LoadBalancingTestBase
    {
        private readonly IWindsorContainer container;

        public Full_test_of_load_balancer_and_failover()
        {
            var interpreter = new XmlInterpreter(@"LoadBalancer\BusWithLoadBalancer.config");
            container = new WindsorContainer(interpreter);
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());

            container.Register(
                Component.For<MsmqLoadBalancer>()
                    .DependsOn(new
                    {
                        threadCount = 1,
                        endpoint = new Uri(loadBalancerQueue),
                        SecondaryLoadBalancer = TestQueueUri2.Uri
                    }),
                Component.For<MsmqSecondaryLoadBalancer>()
                    .DependsOn(new
                    {
                        threadCount = 1,
                        endpoint = TestQueueUri2.Uri,
                        primaryLoadBalancer = new Uri(loadBalancerQueue),
                    })
                );
        }

        [Fact(Skip = "Not working yet")]
        public void Can_send_messages_to_worker()
        {
            using (var transport = container.Resolve<ITransport>())
            using (var bus = container.Resolve<IStartableServiceBus>())
            using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
            using (var secondaryLoadBalancer = container.Resolve<MsmqSecondaryLoadBalancer>())
            {
                var wait = new ManualResetEvent(false);
                transport.MessageArrived += information => wait.Set();
                
                bus.Start();

                loadBalancer.Start();

                secondaryLoadBalancer.Start();

                transport.Send(loadBalancer.Endpoint, "test");

                Assert.True(wait.WaitOne(TimeSpan.FromSeconds(30), false));
            }
        }

        [Fact(Skip = "Not working yet")]
        public void Can_send_messages_to_worker_even_when_load_balancer_goes_does()
        {
            using (var transport = container.Resolve<ITransport>())
            using (var bus = container.Resolve<IStartableServiceBus>())
            using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
            using (var secondaryLoadBalancer = container.Resolve<MsmqSecondaryLoadBalancer>())
            {
                var waitForReroute = new ManualResetEvent(false);
                var waitForMsg = new ManualResetEvent(false);
                transport.MessageArrived += information => waitForMsg.Set();
                bus.ReroutedEndpoint += reroute => waitForReroute.Set();

                secondaryLoadBalancer.TimeoutForHeartBeatFromPrimary = TimeSpan.FromSeconds(2);

                bus.Start();

                loadBalancer.Start();

                secondaryLoadBalancer.Start();

                transport.Send(loadBalancer.Endpoint, "test");

                Assert.True(waitForMsg.WaitOne(TimeSpan.FromSeconds(30), false));

                loadBalancer.Dispose();

                Assert.True(waitForReroute.WaitOne(TimeSpan.FromSeconds(30), false));

                waitForMsg.Reset();

                transport.Send(loadBalancer.Endpoint, "test2");

                Assert.True(waitForMsg.WaitOne(TimeSpan.FromSeconds(30), false));
                Assert.True(secondaryLoadBalancer.TookOverWork);
            }
        }
    }
}