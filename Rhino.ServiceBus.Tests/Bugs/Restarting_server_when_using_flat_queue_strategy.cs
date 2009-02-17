using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class Restarting_server_when_using_flat_queue_strategy : MsmqTestBase
    {
        [Fact]
        public void Can_restart_bus()
        {
            using(var bus = CreateContainer().Resolve<IStartableServiceBus>())
            {
                bus.Start();
            }

            using (var bus = CreateContainer().Resolve<IStartableServiceBus>())
            {
                bus.Start();
            }
        }

        private WindsorContainer CreateContainer()
        {
            var container = new WindsorContainer(new XmlInterpreter());
            var facility = new RhinoServiceBusFacility().UseFlatQueueStructure();
            container.Kernel.AddFacility("rhino.esb", facility);
            return container;
        }
    }
}