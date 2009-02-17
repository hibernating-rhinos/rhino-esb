using System;
using System.Messaging;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Msmq
{
	using System.Collections.Generic;

	/// <summary>
	/// Handles message moving to sibling queues.
	/// Suitable for MSMQ 3.0
	/// </summary>
	/// <remarks>
	/// <para>
	/// This strategy presumes additional queues than those defined by the endpoint.
	/// </para>
	/// <list type="bullet">
	/// <listheader>So your queue structure would be:</listheader>
	/// <item>[my_queue_name]</item>
	/// <item>[my_queue_name]<c>#subscriptions</c></item>
	/// <item>[my_queue_name]<c>#errors</c></item>
	/// <item>[my_queue_name]<c>#discarded</c></item>
	/// <item>[my_queue_name]<c>#timeout</c></item>
	/// </list>
	/// </remarks>
	public class FlatQueueStrategy : IQueueStrategy
	{
	    private readonly IEndpointRouter endpointRouter;
	    private readonly Uri endpoint;
		private const string subscriptions = "#subscriptions";
		private const string errors = "#errors";
		private const string timeout = "#timeout";
		private const string discarded = "#discarded";

		/// <summary>
		/// Initializes a new instance of the <see cref="FlatQueueStrategy"/> class.
		/// </summary>
		public FlatQueueStrategy(IEndpointRouter endpointRouter,Uri endpoint)
		{
		    this.endpointRouter = endpointRouter;
		    this.endpoint = endpoint;
		}

	    public void InitializeQueue(Endpoint queueEndpoint)
		{
			var accessMode = QueueAccessMode.SendAndReceive;
			MsmqUtil.GetQueuePath(queueEndpoint).Create();
			
			MsmqUtil.CreateQueue(GetErrorsQueuePath(), accessMode);
			MsmqUtil.CreateQueue(GetSubscriptionQueuePath(), accessMode);
			MsmqUtil.CreateQueue(GetDiscardedQueuePath(), accessMode);
			MsmqUtil.CreateQueue(GetTimeoutQueuePath(), accessMode);
		}

		/// <summary>
		/// Creates the subscription queue URI.
		/// </summary>
		/// <param name="subscriptionQueue">The subscription queue.</param>
		/// <returns></returns>
		public Uri CreateSubscriptionQueueUri(Uri subscriptionQueue)
		{
			return new Uri(subscriptionQueue + "#subscriptions");
		}


		/// <summary>
		/// Gets a listing of all timeout messages.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<TimeoutInfo> GetTimeoutMessages(OpenedQueue queue)
		{
			yield break;
		}

		/// <summary>
		/// Moves the message from the timeout queue to the main queue.
		/// </summary>
		/// <param name="queue">The queue.</param>
		/// <param name="messageId">The message id.</param>
		public void MoveTimeoutToMainQueue(OpenedQueue queue, string messageId)
		{
			using (var destinationQueue = new MessageQueue(GetTimeoutQueuePath(), QueueAccessMode.Receive))
			{
				var message = destinationQueue.ReceiveById(messageId);
				message.AppSpecific = 0;//reset timeout flag
				queue.Send(message);
			}
		}

	    public bool TryMoveMessage(OpenedQueue queue, Message message, SubQueue subQueue, out string msgId)
	    {
            using (var destinationQueue = queue.OpenSiblngQueue(subQueue, QueueAccessMode.Send))
            {
                Message receiveById;
                try
                {
                    receiveById = queue.ReceiveById(message.Id);
                }
                catch (InvalidOperationException)
                {
                    msgId = null;
                    return false;
                }
                message.AppSpecific = 0;//reset flag
                destinationQueue.Send(receiveById);
                msgId = receiveById.Id;
                return true;
            }
	    }

	    /// <summary>
		/// Gets the errors queue path.
		/// </summary>
		/// <returns></returns>
		private string GetErrorsQueuePath()
		{
            var path = MsmqUtil.GetQueuePath(endpointRouter.GetRoutedEndpoint(endpoint));
			return path.QueuePath + errors;
		}

		/// <summary>
		/// Gets the discarded queue path.
		/// </summary>
		/// <returns></returns>
		private string GetDiscardedQueuePath()
		{
            var path = MsmqUtil.GetQueuePath(endpointRouter.GetRoutedEndpoint(endpoint));
			return path.QueuePath + discarded;
		}

		/// <summary>
		/// Gets the timeout queue path.
		/// </summary>
		/// <returns></returns>
		private string GetTimeoutQueuePath()
		{
            var path = MsmqUtil.GetQueuePath(endpointRouter.GetRoutedEndpoint(endpoint));
			return path.QueuePath + timeout;
		}

		private string GetSubscriptionQueuePath()
		{
            var path = MsmqUtil.GetQueuePath(endpointRouter.GetRoutedEndpoint(endpoint));
			return path.QueuePath + subscriptions;
		}
	}

}
