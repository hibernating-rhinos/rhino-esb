using System;
using System.Messaging;
using Common.Logging;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Transport;

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
        private readonly ILog logger = LogManager.GetLogger(typeof(FlatQueueStrategy));
        private readonly IEndpointRouter endpointRouter;
        private readonly Uri endpoint;
        private const string subscriptions = "#subscriptions";
        private const string errors = "#errors";
        private const string timeout = "#timeout";
        private const string discarded = "#discarded";
        private const string knownEndpoints = "#endpoints";
        private const string knownWorkers = "#workers";

        /// <summary>
        /// Initializes a new instance of the <see cref="FlatQueueStrategy"/> class.
        /// </summary>
        public FlatQueueStrategy(IEndpointRouter endpointRouter, Uri endpoint)
        {
            this.endpointRouter = endpointRouter;
            this.endpoint = endpoint;
        }

        public MessageQueue[] InitializeQueue(Endpoint queueEndpoint, QueueType queueType)
        {
        	var queue = MsmqUtil.GetQueuePath(queueEndpoint).Create();
        	switch (queueType)
            {
                case QueueType.Standard:
                    return new[]
	                {
	                    queue,
	                    MsmqUtil.OpenOrCreateQueue(GetErrorsQueuePath(), QueueAccessMode.SendAndReceive, queue),
	                    MsmqUtil.OpenOrCreateQueue(GetSubscriptionQueuePath(), QueueAccessMode.SendAndReceive, queue),
	                    MsmqUtil.OpenOrCreateQueue(GetDiscardedQueuePath(), QueueAccessMode.SendAndReceive, queue),
	                    MsmqUtil.OpenOrCreateQueue(GetTimeoutQueuePath(), QueueAccessMode.SendAndReceive, queue),
	                };
                case QueueType.LoadBalancer:
                    return new[]
	                {
	                    queue,
	                    MsmqUtil.OpenOrCreateQueue(GetKnownWorkersQueuePath(), QueueAccessMode.SendAndReceive, queue),
	                    MsmqUtil.OpenOrCreateQueue(GetKnownEndpointsQueuePath(), QueueAccessMode.SendAndReceive, queue),
	                };
                case QueueType.Raw:
                    return new[]
	                {
	                    queue,
	                };
                default:
                    throw new ArgumentOutOfRangeException("queueType", "Can't handle queue type: " + queueType);
            }
        }

    	private string GetKnownEndpointsQueuePath()
        {
            var path = MsmqUtil.GetQueuePath(endpointRouter.GetRoutedEndpoint(endpoint));
            return path.QueuePath + knownEndpoints;
        }

        private string GetKnownWorkersQueuePath()
        {
            var path = MsmqUtil.GetQueuePath(endpointRouter.GetRoutedEndpoint(endpoint));
            return path.QueuePath + knownWorkers;
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
        	using (var timeoutQueue = queue.OpenSiblngQueue(SubQueue.Timeout, QueueAccessMode.Receive))
        	{
        		var enumerator2 = timeoutQueue.GetMessageEnumerator2();
        		while (enumerator2.MoveNext())
        		{
        			var message = enumerator2.Current;
							if (message == null || message.Extension.Length < 16)
							{
								continue;
							}

        			yield return new TimeoutInfo
        			             	{
        			             		Id = message.Id,
        			             		Time = DateTime.FromBinary(BitConverter.ToInt64(message.Extension, 16))
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
            using (var destinationQueue = new MessageQueue(GetTimeoutQueuePath(), QueueAccessMode.Receive))
            {
                destinationQueue.MessageReadPropertyFilter.SetAll();
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
                receiveById.AppSpecific = 0;//reset flag
                destinationQueue.Send(receiveById);
                msgId = receiveById.Id;
                logger.DebugFormat("Moving messgage {0} from {1} to {2}, new id: {3}",
                    message.Id,
                    queue.RootUri,
                    destinationQueue.QueueUrl,
                    receiveById.Id);
                return true;
            }
        }

    	public void SendToErrorQueue(OpenedQueue queue, Message message)
    	{
    		using(var errQueue = new MessageQueue(GetErrorsQueuePath()))
    		{
				// here we assume that the queue transactionalibilty is the same for the error sibling queue
				// and the main queue!
    			errQueue.Send(message, queue.GetSingleTransactionType());
    		}
    	}

        public OpenedQueue OpenSubQueue(OpenedQueue queue, SubQueue subQueue, QueueAccessMode accessMode)
        {
            return queue.OpenSiblngQueue(subQueue, accessMode);
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
