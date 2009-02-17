using System;
using Rhino.ServiceBus;
using Rhino.ServiceBus.Util;
using Starbucks.Messages.Cashier;

namespace Starbucks.Cashier
{
    public class CashierSaga :
        ConsumerOf<NewOrder>,
        ConsumerOf<SubmitPayment>
    {
        private readonly IServiceBus bus;

        public CashierSaga(IServiceBus bus)
        {
            this.bus = bus;
        }

        #region InitiatedBy<NewOrder> Members

        public void Consume(NewOrder message)
        {
            Console.WriteLine("Cashier: got new order");
            var correlationId = GuidCombGenerator.Generate();
            bus.Publish(new PrepareDrink
            {
                CorrelationId = correlationId,
                CustomerName = message.CustomerName,
                DrinkName = message.DrinkName,
                Size = message.Size
            });
            bus.Reply(new PaymentDue
            {
                CustomerName = message.CustomerName,
                StarbucksTransactionId = correlationId,
                Amount = ((int) message.Size)*1.25m
            });
        }

        #endregion

        #region Orchestrates<SubmitPayment> Members

        public void Consume(SubmitPayment message)
        {
            Console.WriteLine("Cashier: got payment");
            bus.Publish(new PaymentComplete
            {
                CorrelationId = message.CorrelationId
            });
        }

        #endregion
    }
}