using System;
using System.Messaging;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Actions
{
    public class CreateQueuesAction : IDeploymentAction
    {
        private readonly IQueueStrategy queueStrategy;
        private readonly IServiceBus serviceBus;

        public CreateQueuesAction(IQueueStrategy queueStrategy, IServiceBus serviceBus)
        {
            this.queueStrategy = queueStrategy;
            this.serviceBus = serviceBus;
        }

        public void Execute(string user)
        {
            // will create the queues if they are not already there
            var queues = queueStrategy.InitializeQueue(serviceBus.Endpoint);
            foreach (var queue in queues)
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