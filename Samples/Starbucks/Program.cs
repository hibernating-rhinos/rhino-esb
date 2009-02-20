using System;
using System.IO;
using log4net;
using log4net.Config;
using Rhino.ServiceBus;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.LoadBalancer;
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
            PrepareQueues.Prepare("msmq://localhost/starbucks.barista.balancer");
            PrepareQueues.Prepare("msmq://localhost/starbucks.cashier");
            PrepareQueues.Prepare("msmq://localhost/starbucks.customer");


            var baristaLoadBalancer = new RemoteAppDomainHost(typeof(RemoteAppDomainHost).Assembly,"LoadBalancer.config")
                .SetHostType(typeof(LoadBalancerHost));
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
