using System;

using System.Messaging;
using System.Transactions;
using log4net;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Transport;
using MessageType = Rhino.ServiceBus.Transport.MessageType;

namespace Rhino.ServiceBus.LoadBalancer
{
	public class MsmqReadyForWorkListener : AbstractMsmqListener
	{
		private readonly ILog logger = LogManager.GetLogger(typeof(MsmqReadyForWorkListener));

		public event Action<ReadyToWork> ReadyToWorkMessageArrived;

		public MsmqReadyForWorkListener(IQueueStrategy queueStrategy, 
			Uri endpoint, 
			int threadCount, 
			IMessageSerializer messageSerializer, 
			IEndpointRouter endpointRouter, 
			TransactionalOptions transactional,
            IMessageBuilder<Message> messageBuilder) : base(queueStrategy, endpoint, threadCount, messageSerializer, endpointRouter, transactional, messageBuilder)
		{}

		protected override void HandlePeekedMessage(OpenedQueue queue, Message message)
		{
			try
			{
				using (var tx = new TransactionScope(TransactionScopeOption.Required, TransportUtil.GetTransactionTimeout()))
				{
					message = queue.TryGetMessageFromQueue(message.Id);
					if (message == null)
						return;

					if ((MessageType) message.AppSpecific == MessageType.LoadBalancerMessageMarker)
					{
						HandleLoadBalancerMessage(queue, message);
					}
				tx.Complete();
				}
			}
			catch (Exception e)
			{
				logger.Error("Fail to process load balanced message properly", e);
			}
		}

		private void HandleLoadBalancerMessage(OpenedQueue queue, Message message)
		{
			foreach (var msg in DeserializeMessages(queue, message, null))
			{
				var work = msg as ReadyToWork;
				if (work != null)
				{
					var copy = ReadyToWorkMessageArrived;
					if(copy != null)
					{
						copy(work);
					}
				}
			}
		}
	}
}
