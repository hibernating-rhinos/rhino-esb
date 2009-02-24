using System;
using System.Messaging;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Actions
{
    public class CreateLoadBalancerQueuesAction : IDeploymentAction
    {
        private IQueueStrategy queueStrategy;
        private MsmqLoadBalancer loadBalancer;

        public CreateLoadBalancerQueuesAction(IQueueStrategy queueStrategy, MsmqLoadBalancer loadBalancer)
        {
            this.queueStrategy = queueStrategy;
            this.loadBalancer = loadBalancer;
        }

        public void Execute(string user)
        {
            // will create the queues if they are not already there
            var queues = queueStrategy.InitializeQueue(loadBalancer.Endpoint, QueueType.LoadBalancer);
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