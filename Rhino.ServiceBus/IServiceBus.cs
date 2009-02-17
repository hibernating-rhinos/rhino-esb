using System;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus
{
    /// <summary>
    /// The service bus abstraction, allow to publish messages and subscribe
    /// or unsubscribe to messages
    /// </summary>
    public interface IServiceBus
    {

        /// <summary>
        /// Occurs when the bus has rerouted an endpoint
        /// </summary>
        event Action<Reroute> ReroutedEndpoint;

        /// <summary>
        /// Publish a message to all subscribers.
        /// If there are no subscribers, it will throw.
        /// </summary>
        /// <param name="messages"></param>
        void Publish(params object[] messages);

        /// <summary>
        /// Publish a message to all subscribers.
        /// If there are no subscribers, it ignore the message
        /// </summary>
        /// <param name="messages"></param>
        void Notify(params object[] messages);

        /// <summary>
        /// Reply to the source of the current message
        /// Will throw if not currently handling a message
        /// </summary>
        /// <param name="messages"></param>
        void Reply(params object[] messages);

        /// <summary>
        /// Send the message directly to the specified endpoint
        /// </summary>
        void Send(Endpoint endpoint, params object[] messages);

        /// <summary>
        /// Send the message directly to the default endpoint
        /// for this type of message
        /// </summary>
        void Send(params object[] messages);

        /// <summary>
        /// Get the endpoint of the bus
        /// </summary>
        Endpoint Endpoint { get; }

        /// <summary>
        /// Create a weak reference subscription for all the registered consumers 
        /// for this consumer instance
        /// </summary>
        IDisposable AddInstanceSubscription(IMessageConsumer consumer);

        /// <summary>
        /// Subscribe this endpoint to the message type
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        void Subscribe<T>();

        /// <summary>
        /// Subscribe this endpoint to the message type
        /// </summary>
        void Subscribe(Type type);

        /// <summary>
        /// Unsubscribe this endpoint from the message type
        /// </summary>
        void Unsubscribe<T>();

        /// <summary>
        /// Unsubscribe this endpoint from the message type
        /// </summary>
        void Unsubscribe(Type type);

		/// <summary>
		/// Handles the current message later.
		/// </summary>
    	void HandleCurrentMessageLater();

		/// <summary>
		/// Send the message with a built in delay in its processing
		/// </summary>
		/// <param name="endpoint">The endpoint.</param>
		/// <param name="time">The time.</param>
		/// <param name="msgs">The messages.</param>
		void DelaySend(Endpoint endpoint, DateTime time, params object[] msgs);

        /// <summary>
        /// Send the message with a built in delay in its processing
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="msgs">The messages.</param>
        void DelaySend(DateTime time, params object[] msgs);
    }
}