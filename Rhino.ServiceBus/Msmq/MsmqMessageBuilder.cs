using System;
using System.Collections.Specialized;
using System.Linq;
using System.Messaging;
using System.Runtime.Serialization;
using log4net;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Util;

namespace Rhino.ServiceBus.Msmq
{
    public class MsmqMessageBuilder : IMessageBuilder<Message>
    {
        private readonly ILog logger = LogManager.GetLogger(typeof (MsmqMessageBuilder));
        private readonly IMessageSerializer messageSerializer;
        private readonly ICustomizeMessageHeaders[] customizeHeaders;
        private Endpoint endpoint;

        
        public MsmqMessageBuilder(IMessageSerializer messageSerializer, IServiceLocator serviceLocator)
        {
            this.messageSerializer = messageSerializer;
            customizeHeaders = serviceLocator.ResolveAll<ICustomizeMessageHeaders>().ToArray();
        }

        public event Action<Message> MessageBuilt;

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

            byte[] extension;
            var messageId = Guid.NewGuid().ToByteArray();

            if (customizeHeaders.Length > 0)
            {
                var headers = new NameValueCollection();
                foreach (var customizeHeader in customizeHeaders)
                {
                    customizeHeader.Customize(headers);
                }
                var headerBytes = headers.SerializeHeaders();
                //accounts for existing use of Extension for messageId and deferred messages
                extension = new byte[24 + headerBytes.Length];
                Buffer.BlockCopy(messageId, 0, extension, 0, messageId.Length);
                Buffer.BlockCopy(headerBytes, 0, extension, 24, headerBytes.Length);
            }
            else
            {
                extension = messageId;
            }

            message.Extension = extension;

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

            var copy = MessageBuilt;
            if (copy != null)
                copy(message);

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