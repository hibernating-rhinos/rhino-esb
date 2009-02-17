using System;
using System.IO;
using System.Messaging;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
	public class QueueCreationModuleTests : IDisposable
	{
		private readonly Endpoint endPoint = new Uri("msmq://localhost/init_test").ToEndpoint();
		private WindsorContainer container;

		public QueueCreationModuleTests()
		{
			CleanQueue();
		}

		#region IDisposable Members

		public void Dispose()
		{
			CleanQueue();
		}

		#endregion

		[Fact]
		public void Should_create_subqueue_strategy_queues()
		{
			container = new WindsorContainer(new XmlInterpreter(
			                                 	Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InitBus.config")
			                                 	));
			container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility().UseSubqueuesQueueStructure());
			using (var bus = container.Resolve<IStartableServiceBus>())
			{
				bus.Start();
				Assert.True(MsmqUtil.GetQueuePath(endPoint).Exists);
			}
		}

		[Fact]
		public void Should_create_flat_queue_strategy_queues()
		{
			container = new WindsorContainer(new XmlInterpreter(
			                                 	Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InitBus.config")
			                                 	));
			container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility().UseFlatQueueStructure());
			using (var bus = container.Resolve<IStartableServiceBus>())
			{
				bus.Start();

				Assert.True(MsmqUtil.GetQueuePath(endPoint).Exists);
				Assert.True(MessageQueue.Exists(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#subscriptions"));
				Assert.True(MessageQueue.Exists(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#errors"));
				Assert.True(MessageQueue.Exists(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#discarded"));
				Assert.True(MessageQueue.Exists(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#timeout"));
			}
			CleanQueue();
		}

		private void CleanQueue()
		{
			MsmqUtil.GetQueuePath(endPoint).Delete();

			if (MessageQueue.Exists(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#subscriptions"))
			{
				MessageQueue.Delete(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#subscriptions");
			}
			if (MessageQueue.Exists(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#errors"))
			{
				MessageQueue.Delete(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#errors");
			}
			if (MessageQueue.Exists(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#discarded"))
			{
				MessageQueue.Delete(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#discarded");
			}
			if (MessageQueue.Exists(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#timeout"))
			{
				MessageQueue.Delete(MsmqUtil.GetQueuePath(endPoint).QueuePath + "#timeout");
			}
		}
	}
}