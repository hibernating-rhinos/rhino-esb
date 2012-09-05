using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Actions
{
    public class CreateQueuesAction : AbstractCreateQueuesAction
    {
        private readonly IServiceBus serviceBus;

        public CreateQueuesAction(IQueueStrategy queueStrategy, IServiceBus serviceBus)
            : base(queueStrategy)
        {
            this.serviceBus = serviceBus;
        }

        public override void Execute(string user)
        {
            this.CreateQueues(QueueType.Standard, serviceBus.Endpoint, user);
        }
    }
}