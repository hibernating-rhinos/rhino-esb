using System;
using System.Messaging;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Transport;

namespace Rhino.ServiceBus.Msmq
{
	using System.Collections.Generic;

	/// <summary>
	/// Default subqueue stragey enabled in MSMQ 4.0
	/// </summary>
	public class SubQueueStrategy : IQueueStrategy
	{
		public MessageQueue[] InitializeQueue(Endpoint queueEndpoint, QueueType queueType)
		{
			var path = MsmqUtil.GetQueuePath(queueEndpoint);
		    return new[]
		    {
		        path.Create()
		    };
		}

		/// <summary>
		/// Creates the subscription queue URI.
		/// </summary>
		/// <param name="subscriptionQueue">The subscription queue.</param>
		/// <returns></returns>
		public Uri CreateSubscriptionQueueUri(Uri subscriptionQueue)
		{
			return new Uri(subscriptionQueue + ";subscriptions");
		}

		/// <summary>
		/// Gets a listing of all timeout messages.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<TimeoutInfo> GetTimeoutMessages(OpenedQueue queue)
		{
			using (var timeoutQueue = queue.OpenSubQueue(SubQueue.Timeout,QueueAccessMode.Receive))
			{
				var enumerator2 = timeoutQueue.GetMessageEnumerator2();
				while(enumerator2.MoveNext())
				{
					var message = enumerator2.Current;
					if(message==null)
						continue;
					
					yield return new TimeoutInfo
					{
						Id = message.Id,
						Time = DateTime.FromBinary(BitConverter.ToInt64(message.Extension, 0))
					};
				}
			}
		}

		/// <summary>
		/// Moves the message from the timeout queue to the main queue.
		/// </summary>
		/// <param name="queue">The queue.</param>
		/// <param name="messageId">The message id.</param>
		public void MoveTimeoutToMainQueue(OpenedQueue queue, string messageId)
		{
			using (var timeoutQueue = queue.OpenSubQueue(SubQueue.Timeout, QueueAccessMode.Receive))
			{
				var message = timeoutQueue.ReceiveById(messageId);
				message.AppSpecific = 0;//reset timeout flag
				queue.Send(message);
			}
		}

		public bool TryMoveMessage(OpenedQueue queue, Message message, SubQueue subQueue, out string msgId)
	    {
	        try
	        {
	            queue.MoveToSubQueue(subQueue, message);
	            msgId = message.Id;
	            return true;
	        }
	        catch (TransportException)
	        {
	            msgId = null;
	            return false;
	        }
	    }
	}
}
