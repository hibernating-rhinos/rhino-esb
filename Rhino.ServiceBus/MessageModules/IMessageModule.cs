using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.MessageModules
{
    public interface IMessageModule
    {
        void Init(ITransport transport, IServiceBus bus);
		void Stop(ITransport transport, IServiceBus bus);
    }
}