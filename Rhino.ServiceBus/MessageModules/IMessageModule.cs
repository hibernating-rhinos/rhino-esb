using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.MessageModules
{
    public interface IMessageModule
    {
        void Init(ITransport transport);
        void Stop(ITransport transport);
    }
}