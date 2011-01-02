using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class Will_dispose_subscription_storage_when_bus_disposed
    {
        [Fact]
        public void Create_two_instances_of_bus_should_not_fail()
        {
            using(var bus = CreateServiceBus())
            {
            }
            using(var bus = CreateServiceBus())
            {
                Assert.NotNull(bus);
            }
        }

        private static IStartableServiceBus CreateServiceBus()
        {
            var windsorContainer = new WindsorContainer();
            new RhinoServiceBusFacility()
                .UseCastleWindsor(windsorContainer)
                .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
                .Configure();
            var serviceBus = windsorContainer.Resolve<IStartableServiceBus>();
            serviceBus.Start();
            return serviceBus;
        }
    }
}