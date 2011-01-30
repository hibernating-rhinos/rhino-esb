using System;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests.LoadBalancer
{
    public class Full_test_of_load_balancer_and_failover : LoadBalancingTestBase
    {
        private readonly IWindsorContainer container;

        public Full_test_of_load_balancer_and_failover()
        {
            container = new WindsorContainer();

            container.Register(
                Component.For<MsmqLoadBalancer>()
                    .DependsOn(new
                    {
                        threadCount = 1,
                        endpoint = new Uri(loadBalancerQueue),
                        secondaryLoadBalancer = TestQueueUri2.Uri,
                   		transactional = TransactionalOptions.FigureItOut
				 }),
                Component.For<MsmqSecondaryLoadBalancer>()
                    .DependsOn(new
                    {
                        threadCount = 1,
                        endpoint = TestQueueUri2.Uri,
                        primaryLoadBalancer = new Uri(loadBalancerQueue),
						transactional = TransactionalOptions.FigureItOut
                    })
                );
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .UseStandaloneConfigurationFile(@"LoadBalancer\BusWithLoadBalancer.config")
                .Configure();
        }

        [Fact]
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

                transport.Send(loadBalancer.Endpoint, new object[] { "test" });

                Assert.True(wait.WaitOne(TimeSpan.FromSeconds(30), false));
            }
        }

        [Fact]
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

                secondaryLoadBalancer.TimeoutForHeartBeatFromPrimary = TimeSpan.FromSeconds(1);

                bus.Start();

                loadBalancer.Start();

                secondaryLoadBalancer.Start();

                transport.Send(loadBalancer.Endpoint, new object[] { "test" });

                Assert.True(waitForMsg.WaitOne(TimeSpan.FromSeconds(10), false));

                loadBalancer.Dispose();

                Assert.True(waitForReroute.WaitOne(TimeSpan.FromSeconds(10), false));

                waitForMsg.Reset();

                transport.Send(loadBalancer.Endpoint, new object[] { "test2" });

                Assert.True(waitForMsg.WaitOne(TimeSpan.FromSeconds(10), false));
                Assert.True(secondaryLoadBalancer.TookOverWork);
            }
        }
    }
}