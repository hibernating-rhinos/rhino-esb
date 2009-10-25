using System;
using System.Linq;
using System.Messaging;
using System.Runtime.Serialization;
using log4net;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus.Msmq
{
    public class MessageBuilder
    {
        private ILog logger = LogManager.GetLogger(typeof (MessageBuilder));
        private readonly IMessageSerializer messageSerializer;
        private readonly Endpoint endpoint;

        public MessageBuilder(IMessageSerializer messageSerializer, Endpoint endpoint)
        {
            this.messageSerializer = messageSerializer;
            this.endpoint = endpoint;
        }

        public Message GenerateMsmqMessageFromMessageBatch(params object[] msgs)
        {
            var message = new Message();

            var isAdmin = msgs.Any(x => x is AdministrativeMessage);
            try
            {
                messageSerializer.Serialize(msgs, message.BodyStream);
            }
            catch (SerializationException ex)
            {
                logger.Error("Error when trying to serialize message.", ex);
                throw;
            }
            message.Priority = isAdmin ? MessagePriority.High : MessagePriority.Normal;
            if (endpoint != null)
                message.ResponseQueue = endpoint.InitalizeQueue().ToResponseQueue();
            else
                message.ResponseQueue = null;

            message.Extension = Guid.NewGuid().ToByteArray();

            message.AppSpecific = GetAppSpecificMarker(msgs);

            message.Label = msgs
                .Where(msg => msg != null)
                .Select(msg =>
                {
                    string s = msg.ToString();
                    if (s.Length > 249)
                        return s.Substring(0, 246) + "...";
                    return s;
                })
                .FirstOrDefault();
            return message;
        }

        protected static int GetAppSpecificMarker(object[] msgs)
        {
            var msg = msgs[0];
            if (msg is AdministrativeMessage)
                return (int)Transport.MessageType.AdministrativeMessageMarker;
            if (msg is LoadBalancerMessage)
                return (int)Transport.MessageType.LoadBalancerMessageMarker;
            return 0;
        }
    }
}