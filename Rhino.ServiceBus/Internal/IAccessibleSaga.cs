using System;

namespace Rhino.ServiceBus.Internal
{
    public interface IAccessibleSaga : IMessageConsumer
    {
        Guid Id { get; set; }
        bool IsCompleted { get; set; }
    }
}