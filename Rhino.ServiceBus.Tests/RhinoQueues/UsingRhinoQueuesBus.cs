using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Transactions;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Tests.Bugs;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
	using Internal;
	using ServiceBus.RhinoQueues;
	using Transport;

	public class UsingRhinoQueuesBus : WithDebugging, IDisposable
	{
		private readonly IWindsorContainer container;
		private readonly IStartableServiceBus bus;

		public UsingRhinoQueuesBus()
		{
			if (Directory.Exists("test.esent"))
				Directory.Delete("test.esent", true);

			if (Directory.Exists("test.esent2"))
				Directory.Delete("test.esent2", true);

			if (Directory.Exists("test_subscriptions.esent"))
				Directory.Delete("test_subscriptions.esent", true);

			StringConsumer.Value = null;
			StringConsumer.Wait = new ManualResetEvent(false);

			container = new WindsorContainer(new XmlInterpreter("RhinoQueues/RhinoQueues.config"));
			container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
			container.AddComponent<WhenTransactionCommitErrors_ShouldNotCrash_Msmq.ConsumerEnlistingInBadTransaction>();
			container.AddComponent<StringConsumer>();
			container.AddComponent<ThrowingIntConsumer>();
			bus = container.Resolve<IStartableServiceBus>();
			bus.Start();
		}

		[Fact]
		public void Can_send_and_receive_messages()
		{
			using (var tx = new TransactionScope())
			{
				bus.Send(bus.Endpoint, "hello");

				tx.Complete();
			}

			Assert.True(StringConsumer.Wait.WaitOne(TimeSpan.FromSeconds(100), false));

			Assert.Equal("hello", StringConsumer.Value);
		}

		[Fact]
		public void Can_handle_errors_gracefully()
		{
			var transport = (RhinoQueuesTransport)container.Resolve<ITransport>();

			using (var tx = new TransactionScope())
			{
				bus.Send(bus.Endpoint, 5);

				tx.Complete();
			}

			using (var tx = new TransactionScope())
			{
				var message = transport.Queue.Receive(SubQueue.Errors.ToString());
				var msgs = container.Resolve<IMessageSerializer>().Deserialize(new MemoryStream(message.Data));
				Assert.Equal(5, msgs[0]);

				tx.Complete();
			}
		}

		[Fact]
		public void Can_survive_errors_during_commit_phase()
		{
			bus.Send(bus.Endpoint, new WhenTransactionCommitErrors_ShouldNotCrash_Msmq.MsgToCreateBadTxEnlistment());

			var transport = (RhinoQueuesTransport) container.Resolve<ITransport>();
			using (var tx = new TransactionScope())
			{
				var message = transport.Queue.Receive(SubQueue.Errors.ToString());
				var msgs = container.Resolve<IMessageSerializer>().Deserialize(new MemoryStream(message.Data));
				Assert.IsType<WhenTransactionCommitErrors_ShouldNotCrash_Msmq.MsgToCreateBadTxEnlistment>(msgs[0]);
				var err = transport.Queue.Receive(SubQueue.Errors.ToString());
				Assert.True(Encoding.Unicode.GetString(err.Data).Contains("blah"));

				tx.Complete();
			}
		}

		public void Dispose()
		{
			container.Dispose();
		}

		public class StringConsumer : ConsumerOf<string>
		{
			public static string Value;
			public static ManualResetEvent Wait;

			public void Consume(string message)
			{
				Value = message;
				Wait.Set();
			}
		}


		public class ThrowingIntConsumer : ConsumerOf<int>
		{
			public void Consume(int message)
			{
				throw new InvalidOperationException("I want to be Long consumer");
			}
		}
	}
}