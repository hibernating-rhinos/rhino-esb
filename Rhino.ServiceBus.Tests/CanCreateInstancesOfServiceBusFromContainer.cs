using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class CanCreateInstancesOfServiceBusFromContainer : MsmqTestBase
    {
        private readonly WindsorContainer container;

        public CanCreateInstancesOfServiceBusFromContainer()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
        }

        [Fact]
        public void Can_resolve_the_service_bus()
        {
            var bus = container.Resolve<IServiceBus>();
            Assert.NotNull(bus);
        }

        [Fact]
        public void Startable_bus_and_service_bus_are_same_instance()
        {
            var bus = container.Resolve<IServiceBus>();
            var startable = container.Resolve<IStartableServiceBus>();
            Assert.Same(bus, startable);
        }

        [Fact]
        public void Can_resolve_startable_service_bus()
        {
            var startable = container.Resolve<IStartableServiceBus>();
            Assert.NotNull(startable);
        }


    }
}