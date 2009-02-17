using System;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Impl
{
    public class InstanceSubscriptionInformation
    {
        public Guid InstanceSubscriptionKey { get; set; }
        public IMessageConsumer Consumer { get; set; }
        public Type[] ConsumedMessages { get; set; }

        public void Dispose()
        {
            Consumer = null;
        }
    }
}