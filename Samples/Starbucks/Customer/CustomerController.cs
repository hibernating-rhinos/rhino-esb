using System;
using System.Threading;
using Rhino.ServiceBus;
using Starbucks.Messages;
using Starbucks.Messages.Barista;
using Starbucks.Messages.Cashier;

namespace Starbucks.Customer
{
    public class CustomerController : 
        OccasionalConsumerOf<PaymentDue>,
        OccasionalConsumerOf<DrinkReady>
    {
        public string Name { get; set; }
        public string Drink { get; set; }
        public DrinkSize Size { get; set; }
        private readonly IServiceBus bus;
        private ManualResetEvent wait;

        public CustomerUserInterface CustomerUserInterface { get; set; }

        public CustomerController(IServiceBus bus)
        {
            CustomerUserInterface = new CustomerUserInterface();
            this.bus = bus;
        }

        public void BuyDrinkSync()
        {
            using(bus.AddInstanceSubscription(this))
            {
                wait = new ManualResetEvent(false);

                bus.Send(new NewOrder {CustomerName = Name, DrinkName = Drink, Size = Size});

                if(wait.WaitOne(TimeSpan.FromSeconds(30))==false)
                    throw new InvalidOperationException("didn't get my coffee in time");
            }
        }

        public void Consume(PaymentDue message)
        {
            if(CustomerUserInterface.ShouldPayForDrink(Name, message.Amount)==false)
                return;

            bus.Reply(new SubmitPayment
            {
                Amount = message.Amount, 
                CorrelationId = message.StarbucksTransactionId
            });
        }

        public void Consume(DrinkReady message)
        {
            CustomerUserInterface.CoffeeRush(Name);
            if (wait != null)
                wait.Set();
        }
    }
}
