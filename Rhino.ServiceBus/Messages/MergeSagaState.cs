using System;
using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus.Messages
{
    public class MergeSagaState : ISagaMessage
    {
        public Guid CorrelationId
        {
            get; set;
        }
    }
}