using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus
{
    public interface ConsumerOf<TMsg> : IMessageConsumer
    {
        void Consume(TMsg message);
    }
}