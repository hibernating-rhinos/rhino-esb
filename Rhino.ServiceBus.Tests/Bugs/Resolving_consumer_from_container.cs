using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class Resolving_consumer_from_container
    {
        private readonly IWindsorContainer container;

        public Resolving_consumer_from_container()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());

            container.AddComponent<SendByMessageOwner.TestHandler>();
        }

        [Fact]
        public void Should_always_give_new_instance()
        {
            Assert.NotSame(
                container.Resolve<SendByMessageOwner.TestHandler>(),
                container.Resolve<SendByMessageOwner.TestHandler>());
        }
    }
}