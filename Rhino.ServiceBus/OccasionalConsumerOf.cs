using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus
{
    public interface OccasionalConsumerOf<TMsg> : ConsumerOf<TMsg>, IOccasionalMessageConsumer
    {
        
    }
}