using System;
using Rhino.ServiceBus;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Msmq;
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
            PrepareQueues.Prepare("msmq://localhost/starbucks.barista.balancer", QueueType.LoadBalancer);
            PrepareQueues.Prepare("msmq://localhost/starbucks.barista.balancer.acceptingwork", QueueType.LoadBalancer);
            PrepareQueues.Prepare("msmq://localhost/starbucks.barista", QueueType.Standard);
            PrepareQueues.Prepare("msmq://localhost/starbucks.cashier", QueueType.Standard);
            PrepareQueues.Prepare("msmq://localhost/starbucks.customer", QueueType.Standard);

            var baristaLoadBalancer = new RemoteAppDomainHost(typeof(CastleBootStrapper).Assembly, "BaristaLoadBalancer.config");
            baristaLoadBalancer.Start();
            
            Console.WriteLine("Barista load balancer has started");

            var cashier = new RemoteAppDomainHost(typeof(CashierBootStrapper))
                .Configuration("Cashier.config");
            cashier.Start();

            Console.WriteLine("Cashier has started");

            var barista = new RemoteAppDomainHost(typeof(BaristaBootStrapper))
                .Configuration("Barista.config");
            barista.Start();

            Console.WriteLine("Barista has started");

            var customerHost = new DefaultHost();

            customerHost.BusConfiguration(c => c.Bus("msmq://localhost/starbucks.customer")
                .Receive("Starbucks.Messages.Cashier", "msmq://localhost/starbucks.cashier")
                .Receive("Starbucks.Messages.Barista", "msmq://localhost/starbucks.barista.balancer"));
            customerHost.Start<CustomerBootStrapper>();

            var bus = (IServiceBus)customerHost.Bus;

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