using System.Messaging;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Actions
{
    public class CreateLogQueueAction : AbstractCreateQueuesAction
    {
        private readonly MessageLoggingModule messageLoggingModule;
        private readonly ITransport transport;

        public CreateLogQueueAction(IQueueStrategy queueStrategy, MessageLoggingModule messageLoggingModule, ITransport transport)
            : base(queueStrategy)
        {
            this.messageLoggingModule = messageLoggingModule;
            this.transport = transport;
        }

        public override void Execute(string user)
        {
            // will create the queues if they are not already there
            messageLoggingModule.Init(transport, null);
            var queuePath = MsmqUtil.GetQueuePath(new Endpoint
            {
                Uri = messageLoggingModule.LogQueue
            }).QueuePath;

            using (var queue = new MessageQueue(queuePath))
            {
                GrantPermissions(queue, user);
            }
        }
    }
}