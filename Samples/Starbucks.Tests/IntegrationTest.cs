using System;
using Rhino.ServiceBus;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Msmq;
using Starbucks.Barista;
using Starbucks.Cashier;
using Starbucks.Customer;
using Starbucks.Messages;
using Xunit;

namespace Starbucks.Tests
{
    public class IntegrationTest : IDisposable
    {
        private readonly RemoteAppDomainHost baristaLoadBalancer;
        private readonly RemoteAppDomainHost cashier;
        private readonly RemoteAppDomainHost barista;
        private readonly DefaultHost customerHost;

        public IntegrationTest()
        {
            PrepareQueues.Prepare("msmq://localhost/starbucks.barista.balancer", QueueType.LoadBalancer);
            PrepareQueues.Prepare("msmq://localhost/starbucks.barista.balancer.acceptingwork", QueueType.LoadBalancer);
            PrepareQueues.Prepare("msmq://localhost/starbucks.barista", QueueType.Standard);
            PrepareQueues.Prepare("msmq://localhost/starbucks.cashier", QueueType.Standard);
            PrepareQueues.Prepare("msmq://localhost/starbucks.customer", QueueType.Standard);

            baristaLoadBalancer = new RemoteAppDomainHost(typeof (CastleLoadBalancerBootStrapper).Assembly, "BaristaLoadBalancer.config");
            cashier = new RemoteAppDomainHost(typeof(CashierBootStrapper))
                .Configuration("Cashier.config");
            barista = new RemoteAppDomainHost(typeof(BaristaBootStrapper))
                .Configuration("Barista.config");
            customerHost = new DefaultHost();
        }

        [Fact]
        public void Can_by_coffee_from_starbucks()
        {
            baristaLoadBalancer.Start();
            Console.WriteLine("Barista load balancer has started");

            cashier.Start();

            Console.WriteLine("Cashier has started");

            barista.Start();

            Console.WriteLine("Barista has started");

            customerHost.Start<CustomerBootStrapper>();

            var bus = (IServiceBus)customerHost.Bus;

            var userInterface = new MockCustomerUserInterface();
            var customer = new CustomerController(bus)
            {
                CustomerUserInterface = userInterface,
                Drink = "Hot Chocolate",
                Name = "Ayende",
                Size = DrinkSize.Venti
            };

            customer.BuyDrinkSync();

            Assert.Equal("Ayende", userInterface.CoffeeRushName);
        }

        public class MockCustomerUserInterface : CustomerUserInterface
        {
            public override bool ShouldPayForDrink(string name, decimal amount)
            {
                return true;
            }

            public string CoffeeRushName;

            public override void CoffeeRush(string name)
            {
                CoffeeRushName = name;
            }
        }


        public void Dispose()
        {
            baristaLoadBalancer.Close();
            customerHost.Dispose();
            cashier.Close();
            barista.Close();
        }
    }
}
