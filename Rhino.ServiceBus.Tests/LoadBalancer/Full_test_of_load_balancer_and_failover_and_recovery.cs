using System;
using System.Messaging;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests.LoadBalancer
{
	public class Full_test_of_load_balancer_and_failover_and_recovery : LoadBalancingTestBase
	{
		private readonly IWindsorContainer container;
		private readonly IWindsorContainer receivingBusContainer;


		public Full_test_of_load_balancer_and_failover_and_recovery()
		{
			var loadBalancerQueuePathUri = new Uri(loadBalancerQueue).ToEndpoint().Uri;
			var lb2Endpoint = new Uri("msmq://localhost/test_queue.balancer2").ToEndpoint();
			var loadBalancerQueuePath2 = MsmqUtil.GetQueuePath(lb2Endpoint).QueuePath;
			var loadBalancerQueuePathUri2 = lb2Endpoint.Uri;

			if (MessageQueue.Exists(loadBalancerQueuePath2))
				MessageQueue.Delete(loadBalancerQueuePath2);
			MessageQueue.Create(loadBalancerQueuePath2, true);

			container = new WindsorContainer(new XmlInterpreter(@"LoadBalancer\SendingBusToLoadBalancer.config"));
			container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
			container.Register(
				Component.For<MsmqLoadBalancer>()
					.DependsOn(new
					{
						endpointRouter = new EndpointRouter(),
						threadCount = 1,
						endpoint = loadBalancerQueuePathUri,
						secondaryLoadBalancer = loadBalancerQueuePathUri2,
						transactional = TransactionalOptions.FigureItOut
					}).LifeStyle.Transient,
				Component.For<MsmqSecondaryLoadBalancer>()
					.DependsOn(new
					{
						endpointRouter = new EndpointRouter(),
						threadCount = 1,
						endpoint = loadBalancerQueuePathUri2,
						primaryLoadBalancer = loadBalancerQueuePathUri,
						transactional = TransactionalOptions.FigureItOut
					})
				);

			//New conatainer to more closely mimic as separate app.
			receivingBusContainer = new WindsorContainer(new XmlInterpreter(@"LoadBalancer\ReceivingBusWithLoadBalancer.config"));
			receivingBusContainer.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
			receivingBusContainer.AddComponent<TestHandler>();
		}

		[Fact]
		public void Can_send_messages_to_worker_even_when_load_balancer_goes_does_then_up_again()
		{
			using (var bus = container.Resolve<IStartableServiceBus>())
			using (var bus2 = receivingBusContainer.Resolve<IStartableServiceBus>())
			{
				bus.Start();
				bus2.Start();
				using (var secondaryLoadBalancer = container.Resolve<MsmqSecondaryLoadBalancer>())
				{
					var waitForTakeover = new ManualResetEvent(false);
					using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
					{
						loadBalancer.Start();
						TestHandler.ResetEvent = new ManualResetEvent(false);
						TestHandler.GotMessage = false;
						bus.Send(new TestMessage());
						TestHandler.ResetEvent.WaitOne(TimeSpan.FromSeconds(5), false);
						
						secondaryLoadBalancer.TimeoutForHeartBeatFromPrimary = TimeSpan.FromSeconds(2);
						secondaryLoadBalancer.TookOverAsActiveLoadBalancer += () => waitForTakeover.Set();
						secondaryLoadBalancer.Start();
						Thread.Sleep(TimeSpan.FromSeconds(1)); //wait for endpoint query to complete.  

					}
					Assert.True(waitForTakeover.WaitOne(TimeSpan.FromSeconds(10), false));

					TestHandler.ResetEvent = new ManualResetEvent(false);
					TestHandler.GotMessage = false;
					bus.Send(new TestMessage());
					TestHandler.ResetEvent.WaitOne(TimeSpan.FromSeconds(30), false);
					Assert.True(TestHandler.GotMessage);
				}

				using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
				{
					var restarted = new ManualResetEvent(false);
					loadBalancer.Started += () => restarted.Set();
					loadBalancer.Start();
					Assert.True(restarted.WaitOne(TimeSpan.FromSeconds(10), false));

					TestHandler.ResetEvent = new ManualResetEvent(false);
					TestHandler.GotMessage = false;
					bus.Send(new TestMessage());
					TestHandler.ResetEvent.WaitOne(TimeSpan.FromSeconds(10), false);
					Assert.True(TestHandler.GotMessage);
				}
			}
		}
	}

	public class TestMessage
	{

	}

	public class TestHandler : ConsumerOf<TestMessage>
	{
		public static bool GotMessage;
		public static ManualResetEvent ResetEvent;

		public void Consume(TestMessage message)
		{
			GotMessage = true;
			ResetEvent.Set();
		}
	}
}