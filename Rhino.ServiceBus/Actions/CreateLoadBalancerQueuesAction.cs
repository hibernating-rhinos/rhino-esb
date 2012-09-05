using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Actions
{
    public class CreateLoadBalancerQueuesAction : AbstractCreateQueuesAction
    {
        private readonly MsmqLoadBalancer loadBalancer;

        public CreateLoadBalancerQueuesAction(IQueueStrategy queueStrategy, MsmqLoadBalancer loadBalancer)
            : base(queueStrategy)
        {
            this.loadBalancer = loadBalancer;
        }

        public override void Execute(string user)
        {
            this.CreateQueues(QueueType.LoadBalancer, loadBalancer.Endpoint, user);
        }
    }
}