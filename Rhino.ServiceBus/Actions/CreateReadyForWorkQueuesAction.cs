using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Actions
{
    public class CreateReadyForWorkQueuesAction : AbstractCreateQueuesAction
    {
        private readonly MsmqReadyForWorkListener readyForWorkListener;

        public CreateReadyForWorkQueuesAction(IQueueStrategy queueStrategy, MsmqReadyForWorkListener readyForWorkListener)
            : base(queueStrategy)
        {
            this.readyForWorkListener = readyForWorkListener;
        }

        public override void Execute(string user)
        {
            this.CreateQueues(QueueType.Raw, readyForWorkListener.Endpoint, user);
        }
    }
}