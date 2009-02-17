using System;
using System.Messaging;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class Messages_that_have_no_handlers : MsmqTestBase
    {

        private readonly IWindsorContainer container;

        public Messages_that_have_no_handlers()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
        }


        [Fact]
        public void Should_go_to_discard_sub_queue()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                bus.Send(bus.Endpoint, "foobar");

                using(var discarded = new MessageQueue(testQueuePath+";discarded"))
                {
                    var message = discarded.Receive(TimeSpan.FromSeconds(30));
                    Assert.NotNull(message);
                }
            }
        }
    }
    public class Flat_queue_structure:MsmqFlatQueueTestBase
    {
        public class Messages_that_have_no_handlers : Flat_queue_structure
        {

            private readonly IWindsorContainer container;

            public Messages_that_have_no_handlers()
            {
                container = new WindsorContainer(new XmlInterpreter());
                container.Kernel.AddFacility("rhino.esb", 
                    new RhinoServiceBusFacility().UseFlatQueueStructure());
            }


            [Fact]
            public void Should_go_to_discard_sibling_queue()
            {
                using (var bus = container.Resolve<IStartableServiceBus>())
                {
                    bus.Start();

                    bus.Send(bus.Endpoint, "foobar");

                    using (var discarded = new MessageQueue(testQueuePath + "#discarded"))
                    {
                        var message = discarded.Receive(TimeSpan.FromSeconds(30));
                        Assert.NotNull(message);
                    }
                }
            }
        }
    }
    
}
