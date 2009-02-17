using System;
using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus.Tests.Dht
{
    public class DrinkReady : ISagaMessage
    {
        public Guid CorrelationId { get; set; }
        public string Drink { get; set; }
    }
}