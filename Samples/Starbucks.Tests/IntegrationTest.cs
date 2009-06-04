using System;
using Rhino.ServiceBus;
using Rhino.ServiceBus.Hosting;
using Starbucks.Barista;
using Starbucks.Cashier;
using Starbucks.Customer;
using Starbucks.Messages;
using Xunit;

namespace Starbucks.Tests
{
    public class IntegrationTest
    {
        [Fact]
        public void Can_by_coffee_from_starbucks()
        {
            PrepareQueues.Prepare("msmq://localhost/starbucks.barista");
            PrepareQueues.Prepare("msmq://localhost/starbucks.barista.balancer");
            PrepareQueues.Prepare("msmq://localhost/starbucks.cashier");
            PrepareQueues.Prepare("msmq://localhost/starbucks.customer");

            var baristaLoadBalancer = new RemoteAppDomainLoadBalancerHost(typeof (RemoteAppDomainHost).Assembly, "LoadBalancer.config");
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

            var userInterface = new MockCustomerUserInterface();
            var customer = new CustomerController(bus)
            {
                CustomerUserInterface = userInterface,
                Drink = "Hot Chocolate",
                Name = "Ayende",
                Size = DrinkSize.Venti
            };

            customer.BuyDrinkSync();

            cashier.Close();
            barista.Close();

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
    }
}
