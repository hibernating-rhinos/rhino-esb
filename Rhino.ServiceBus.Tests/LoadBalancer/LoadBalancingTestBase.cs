using System;
using System.Messaging;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Tests.LoadBalancer
{
    public class LoadBalancingTestBase : MsmqTestBase
    {
        protected const string loadBalancerQueue = "msmq://localhost/test_queue.balancer";
        protected readonly string loadBalancerQueuePath = MsmqUtil.GetQueuePath(new Uri(loadBalancerQueue).ToEndpoint()).QueuePath;

        public LoadBalancingTestBase()
        {
            if (MessageQueue.Exists(loadBalancerQueuePath) == false)
                MessageQueue.Create(loadBalancerQueuePath, true);
            using (var loadBalancer = new MessageQueue(loadBalancerQueuePath, QueueAccessMode.SendAndReceive))
            {
                loadBalancer.Purge();
            }

            using (var loadBalancer = new MessageQueue(loadBalancerQueuePath + ";Workers", QueueAccessMode.SendAndReceive))
            {
                loadBalancer.Purge();
            }

            using (var loadBalancer = new MessageQueue(loadBalancerQueuePath + ";Endpoints", QueueAccessMode.SendAndReceive))
            {
                loadBalancer.Purge();
            }
        }
    }
}