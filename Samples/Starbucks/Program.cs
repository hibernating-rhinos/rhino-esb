using System;
using Rhino.ServiceBus;
using Rhino.ServiceBus.Hosting;
using Starbucks.Barista;
using Starbucks.Cashier;
using Starbucks.Customer;
using Starbucks.Messages;

namespace Starbucks
{
    public class Program
    {
        public static void Main()
        {
            PrepareQueues.Prepare("msmq://localhost/starbucks.barista");
            PrepareQueues.Prepare("msmq://localhost/starbucks.cashier");
            PrepareQueues.Prepare("msmq://localhost/starbucks.customer");

            var cashier = new RemoteAppDomainHost(typeof(CashierBootStrapper))
                .Configuration("Cashier.config");
            cashier.Start();

            Console.WriteLine("Cashier is started");

            var barista = new RemoteAppDomainHost(typeof(BaristaBootStrapper))
                .Configuration("Barista.config");
            barista.Start();

            Console.WriteLine("Barista is started");

            var customerHost = new DefaultHost();
            customerHost.Start<CustomerBootStrapper>();

            var bus = customerHost.Container.Resolve<IServiceBus>();

            var customer = new CustomerController(bus)
            {
                Drink = "Hot Chocolate",
                Name = "Ayende",
                Size = DrinkSize.Venti
            };

            customer.BuyDrinkSync();

            Console.ReadLine();
        }
    }
}
