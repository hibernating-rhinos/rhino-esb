using System;
using System.Messaging;
using System.Transactions;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
	public class WhenTransactionCommitErrors_ShouldNotCrash_Msmq : MsmqTestBase
	{
		public class BadEnlistment : IEnlistmentNotification
		{
			public BadEnlistment()
			{
				Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
			}

			public void Prepare(PreparingEnlistment preparingEnlistment)
			{
				preparingEnlistment.ForceRollback(new InvalidOperationException("blah"));
			}

			public void Commit(Enlistment enlistment)
			{
				enlistment.Done();
			}

			public void Rollback(Enlistment enlistment)
			{
				enlistment.Done();
			}

			public void InDoubt(Enlistment enlistment)
			{
				enlistment.Done();
			}
		}

		public class MsgToCreateBadTxEnlistment{}

		public class ConsumerEnlistingInBadTransaction : ConsumerOf<MsgToCreateBadTxEnlistment>
		{
			public void Consume(MsgToCreateBadTxEnlistment message)
			{
				new BadEnlistment();
			}
		}

		private static IWindsorContainer CreateContainer()
		{
			var container = new WindsorContainer(new XmlInterpreter());
			container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
			container.Kernel.AddComponent<ConsumerEnlistingInBadTransaction>();
			return container;
		}

		[Fact]
		public void Can_survive_errors_during_commit_phase()
		{
			using(var container = CreateContainer())
			using(var bus = container.Resolve<IStartableServiceBus>())
			{
				bus.Start();

				bus.Send(bus.Endpoint, new MsgToCreateBadTxEnlistment());

				using (var errQueue = new MessageQueue(testQueuePath + ";errors"))
				{
					var message = errQueue.Receive();
					Assert.Equal("Rhino.ServiceBus.Tests.Bugs.WhenTransactionCommitErrors_ShouldNotCrash_Msmq+MsgToCreateBadTxEnlistment", message.Label);
					var err = errQueue.Receive();
					Assert.Equal("Error description for: Rhino.ServiceBus.Tests.Bugs.WhenTransactionCommitErrors_ShouldNotCrash_Msmq+MsgToCreateBadTxEnlistment", err.Label);
				}
			}
		}
	}
}