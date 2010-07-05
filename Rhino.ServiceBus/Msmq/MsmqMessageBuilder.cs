using System;
using System.Linq;
using System.Messaging;
using System.Runtime.Serialization;
using log4net;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus.Msmq
{
    public class MsmqMessageBuilder : IMessageBuilder<Message>
    {
        private readonly ILog logger = LogManager.GetLogger(typeof (MsmqMessageBuilder));
        private readonly IMessageSerializer messageSerializer;
        private Endpoint endpoint;

        
        public MsmqMessageBuilder(IMessageSerializer messageSerializer)
        {
            this.messageSerializer = messageSerializer;
        }

        public Message BuildFromMessageBatch(params object[] msgs)
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

        public void Initialize(Endpoint source)
        {
            this.endpoint = source;
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