using System.Collections.Specialized;
using System.Messaging;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Msmq
{
    public class MsmqCurrentMessageInformation : CurrentMessageInformation
    {
        public OpenedQueue Queue { get; set; }
        public Message MsmqMessage { get; set; }
        public NameValueCollection Headers { get; set; }

        public MessageQueueTransactionType TransactionType { get; set; }
    }
}