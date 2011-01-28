using Rhino.ServiceBus.Impl;
using StructureMap;
using Xunit;

namespace Rhino.ServiceBus.Tests.Containers.StructureMap
{
    public class CanCreateInstancesOfOneWayBusFromContainer : MsmqTestBase
    {
        private readonly IContainer container;

        public CanCreateInstancesOfOneWayBusFromContainer()
        {
            container = ObjectFactory.Container;
            new OnewayRhinoServiceBusFacility()
                .UseStructureMap(container)
                .Configure();
        } 

        [Fact]
        public void Can_resolve_the_msmq_transport()
        {
            var oneWayBus = container.GetInstance<IOnewayBus>();
            Assert.NotNull(oneWayBus);
        }
    }
}