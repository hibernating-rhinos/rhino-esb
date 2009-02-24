using System;
using System.Messaging;

namespace Rhino.ServiceBus.Msmq
{
	using System.Collections.Generic;

	/// <summary>
    /// Encapsulates handling of messages based on queue layout
    /// </summary>
    public interface IQueueStrategy
    {
		MessageQueue[] InitializeQueue(Endpoint queueEndpoint, QueueType queueType);
        
        /// <summary>
        /// Creates the subscription queue URI.
        /// </summary>
        /// <param name="subscriptionQueue">The subscription queue.</param>
        /// <returns></returns>
        Uri CreateSubscriptionQueueUri(Uri subscriptionQueue);
        
		/// <summary>
		/// Gets a listing of all timeout messages.
		/// </summary>
		/// <returns></returns>
		IEnumerable<TimeoutInfo> GetTimeoutMessages(OpenedQueue queue);

		/// <summary>
		/// Moves the message from the timeout queue to the main queue.
		/// </summary>
		/// <param name="queue">The queue.</param>
		/// <param name="messageId">The message id.</param>
		void MoveTimeoutToMainQueue(OpenedQueue queue, string messageId);

		bool TryMoveMessage(OpenedQueue queue, Message message, SubQueue subQueue, out string msgId);
    }
}
