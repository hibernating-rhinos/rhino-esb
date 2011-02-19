using System;
using System.Collections.Specialized;
using System.IO;
using Rhino.Queues;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Transport;

namespace Rhino.ServiceBus.RhinoQueues
{
    
    public class RhinoQueuesMessageBuilder : IMessageBuilder<MessagePayload>
    {
        private readonly IMessageSerializer messageSerializer;
        private Endpoint endpoint;
        public RhinoQueuesMessageBuilder(IMessageSerializer messageSerializer)
        {
            this.messageSerializer = messageSerializer;
        }

        public event Action<MessagePayload> MessageBuilt;

        [CLSCompliant(false)]
        public MessagePayload BuildFromMessageBatch(params object[] msgs)
        {
            if (endpoint == null)
                throw new InvalidOperationException("A source endpoint is required for Rhino Queues transport, did you Initialize me? try providing a null Uri.");

            var messageId = Guid.NewGuid();
            byte[] data = new byte[0];
            using (var memoryStream = new MemoryStream())
            {
                messageSerializer.Serialize(msgs, memoryStream);
                data = memoryStream.ToArray();
                
            }
            var payload=new MessagePayload
            {
                Data = data,
                Headers =
                        {
                            {"id", messageId.ToString()},
                            {"type", GetAppSpecificMarker(msgs).ToString()},
                            {"source", endpoint.Uri.ToString()},
                        }
            };

            TryCustomizeHeaders(payload.Headers);

            var copy = MessageBuilt;
            if (copy != null)
                copy(payload);

            return payload;
        }

        private void TryCustomizeHeaders(NameValueCollection headers)
        {
            if (MessageHeaders == null)
                return;
            MessageHeaders.Customize(headers);
        }

        public ICustomizeMessageHeaders MessageHeaders { get; set; }
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