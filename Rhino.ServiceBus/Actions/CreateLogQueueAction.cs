using System;
using System.Messaging;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Actions
{
    public class CreateLogQueueAction : IDeploymentAction
    {
        private readonly MessageLoggingModule messageLoggingModule;
        private readonly ITransport transport;

        public CreateLogQueueAction(MessageLoggingModule messageLoggingModule, ITransport transport)
        {
            this.messageLoggingModule = messageLoggingModule;
            this.transport = transport;
        }

        public void Execute(string user)
        {
            // will create the queues if they are not already there
            messageLoggingModule.Init(transport);
            var queuePath = MsmqUtil.GetQueuePath(new Endpoint
            {
                Uri = messageLoggingModule.LogQueue
            }).QueuePath;

            using (var queue = new MessageQueue(queuePath))
            {
                queue.SetPermissions(user,
                                     MessageQueueAccessRights.DeleteMessage |
                                     MessageQueueAccessRights.DeleteJournalMessage |
                                     MessageQueueAccessRights.GenericRead |
                                     MessageQueueAccessRights.GenericWrite |
                                     MessageQueueAccessRights.GetQueuePermissions |
                                     MessageQueueAccessRights.PeekMessage |
                                     MessageQueueAccessRights.ReceiveJournalMessage |
                                     MessageQueueAccessRights.ReceiveMessage |
                                     MessageQueueAccessRights.WriteMessage,
                                     AccessControlEntryType.Allow);
            }
        }
    }
}