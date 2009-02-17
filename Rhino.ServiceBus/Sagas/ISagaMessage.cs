using System;

namespace Rhino.ServiceBus.Sagas
{
    public interface ISagaMessage
    {
        Guid CorrelationId { get; set; }
    }
}