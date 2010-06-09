using System.Messaging;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Actions
{
    public class CreateReadyForWorkQueuesAction: IDeploymentAction
    {
        private IQueueStrategy queueStrategy;
        private MsmqReadyForWorkListener readyForWorkListener;

        public CreateReadyForWorkQueuesAction(IQueueStrategy queueStrategy, MsmqReadyForWorkListener readyForWorkListener)
        {
            this.queueStrategy = queueStrategy;
            this.readyForWorkListener = readyForWorkListener;
        }

        public void Execute(string user)
        {
            // will create the queues if they are not already there
            var queues = queueStrategy.InitializeQueue(readyForWorkListener.Endpoint, QueueType.Raw);
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