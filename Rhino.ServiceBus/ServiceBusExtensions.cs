using System;

namespace Rhino.ServiceBus
{
    /// <summary>
    /// Provides some convenience extension methods to IServiceBus
    /// </summary>
    public static class ServiceBusExtensions
    {
        /// <summary>
        /// Handles the current message later.
        /// </summary>
        public static void HandleCurrentMessageLater(this IServiceBus serviceBus)
        {
            serviceBus.DelaySend(serviceBus.Endpoint, DateTime.Now, serviceBus.CurrentMessageInformation.Message);
        }

        /// <summary>
        /// Sends the message directly to this bus endpoint
        /// </summary>
        public static void SendToSelf(this IServiceBus serviceBus, params object[] messages)
        {
            serviceBus.Send(serviceBus.Endpoint, messages);
        }

        /// <summary>
        /// Send the message with a built in delay in its processing to this bus endpoint
        /// </summary>
        public static void DelaySendToSelf(this IServiceBus serviceBus, DateTime time, params object[] msgs)
        {
            serviceBus.DelaySend(serviceBus.Endpoint, time, msgs);
        }
    }
}
