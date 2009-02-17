using System;
using Rhino.ServiceBus.Sagas;

namespace Starbucks.Messages.Cashier
{
    public class SubmitPayment : ISagaMessage
    {
        public Guid CorrelationId { get; set; }
        public decimal Amount { get; set; }
    }
}