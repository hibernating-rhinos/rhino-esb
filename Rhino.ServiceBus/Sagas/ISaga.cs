using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Sagas
{
    public interface ISaga<TState> : IAccessibleSaga 
    {
        TState State { get; set; }
    }
}