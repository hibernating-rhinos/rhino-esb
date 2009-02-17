using System;
using Rhino.ServiceBus.Sagas;

namespace Starbucks.Messages.Barista
{
    public class DrinkReady : ISagaMessage
    {
        public Guid CorrelationId { get; set; }
        public string Drink { get; set; }
    }
}
