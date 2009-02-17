using System;
using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus.Tests.Dht
{
    public class PrepareDrink : ISagaMessage
    {
        public string DrinkName { get; set; }

        public string CustomerName { get; set; }

        #region ISagaMessage Members

        public Guid CorrelationId { get; set; }

        #endregion
    }
}