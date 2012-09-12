using System;
using System.IO;
using System.Linq;
using Rhino.Queues;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Transport;

namespace Rhino.ServiceBus.RhinoQueues
{
    public class RhinoQueuesMessageBuilder : IMessageBuilder<MessagePayload>
    {
        private readonly IMessageSerializer messageSerializer;
        private readonly ICustomizeOutgoingMessages[] customizeHeaders;
        private Endpoint endpoint;
        public RhinoQueuesMessageBuilder(IMessageSerializer messageSerializer, IServiceLocator serviceLocator)
        {
            this.messageSerializer = messageSerializer;
            customizeHeaders = serviceLocator.ResolveAll<ICustomizeOutgoingMessages>().ToArray();
        }

        public event Action<MessagePayload> MessageBuilt;

        [CLSCompliant(false)]
        public MessagePayload BuildFromMessageBatch(OutgoingMessageInformation messageInformation)
        {
            if (endpoint == null)
                throw new InvalidOperationException("A source endpoint is required for Rhino Queues transport, did you Initialize me? try providing a null Uri.");

            var messageId = Guid.NewGuid();
            byte[] data = new byte[0];
            using (var memoryStream = new MemoryStream())
            {
                messageSerializer.Serialize(messageInformation.Messages, memoryStream);
                data = memoryStream.ToArray();
                
            }
            var payload=new MessagePayload
            {
                Data = data,
                Headers =
                        {
                            {"id", messageId.ToString()},
                            {"type", GetAppSpecificMarker(messageInformation.Messages).ToString()},
                            {"source", endpoint.Uri.ToString()},
                        }
            };

            messageInformation.Headers = payload.Headers;
            foreach (var customizeHeader in customizeHeaders)
            {
                customizeHeader.Customize(messageInformation);
            }

            payload.DeliverBy = messageInformation.DeliverBy;
            payload.MaxAttempts = messageInformation.MaxAttempts;

            var copy = MessageBuilt;
            if (copy != null)
                copy(payload);

            return payload;
        }
        
        public void Initialize(Endpoint source)
        {
            endpoint = source;
        }

        private static MessageType GetAppSpecificMarker(object[] msgs)
        {
            var msg = msgs[0];
            if (msg is AdministrativeMessage)
                return MessageType.AdministrativeMessageMarker;
            if (msg is LoadBalancerMessage)
                return MessageType.LoadBalancerMessageMarker;
            return 0;
        }
    }
}