using System;
using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus.Tests.Dht
{
    public class PaymentComplete : ISagaMessage
    {
        public Guid CorrelationId { get; set; }
    }
}