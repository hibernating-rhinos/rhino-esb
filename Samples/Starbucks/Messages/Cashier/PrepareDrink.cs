using System;
using Rhino.ServiceBus.Sagas;

namespace Starbucks.Messages.Cashier
{
    public class PrepareDrink : ISagaMessage
    {
        public string DrinkName { get; set; }

        public DrinkSize Size { get; set; }

        public string CustomerName { get; set; }

        #region ISagaMessage Members

        public Guid CorrelationId { get; set; }

        #endregion
    }
}
