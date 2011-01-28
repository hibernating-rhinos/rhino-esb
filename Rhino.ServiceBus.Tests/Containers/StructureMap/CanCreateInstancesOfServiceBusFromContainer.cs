using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Msmq.TransportActions;
using StructureMap;
using Xunit;

namespace Rhino.ServiceBus.Tests.Containers.StructureMap
{
    public class CanCreateInstancesOfServiceBusFromContainer : MsmqTestBase
    {
        private readonly IContainer container;

        public CanCreateInstancesOfServiceBusFromContainer()
        {
            container = ObjectFactory.Container;
            new RhinoServiceBusFacility()
                .UseStructureMap(container)
                .Configure();
        }

        [Fact]
        public void Can_resolve_the_msmq_transport()
        {
            var transport = container.GetAllInstances<IMsmqTransportAction>();
            Assert.NotNull(transport);
        }

        [Fact]
        public void Can_resolve_the_service_bus()
        {
            var bus = container.GetInstance<IServiceBus>();
            Assert.NotNull(bus);
        }

        [Fact]
        public void Startable_bus_and_service_bus_are_same_instance()
        {
            var bus = container.GetInstance<IServiceBus>();
            var startable = container.GetInstance<IStartableServiceBus>();
            Assert.Same(bus, startable);
        }

        [Fact]
        public void Can_resolve_startable_service_bus()
        {
            var startable = container.GetInstance<IStartableServiceBus>();
            Assert.NotNull(startable);
        }
    }
}