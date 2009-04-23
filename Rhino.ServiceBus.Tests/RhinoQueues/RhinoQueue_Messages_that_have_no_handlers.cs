using System;
using System.IO;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.RhinoQueues;
using Rhino.ServiceBus.Tests.RhinoQueues;
using Xunit;
using System.Threading;

namespace Rhino.ServiceBus.Tests
{
	public class RhinoQueues_Messages_that_have_no_handlers : WithDebugging
    {

		public RhinoQueues_Messages_that_have_no_handlers()
        {
            if(Directory.Exists("test.esent"))
                Directory.Delete("test.esent", true);

            if (Directory.Exists("test_subscriptions.esent"))
                Directory.Delete("test_subscriptions.esent", true);

        }

		private static IWindsorContainer CreateContainer()
		{
			var container = new WindsorContainer(new XmlInterpreter("RhinoQueues/RhinoQueues.config"));
			container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
			return container;
		}


		[Fact]
        public void Should_go_to_discard_sub_queue_and_be_able_to_restart_bus()
        {
			using (var container = CreateContainer())
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
            	var wait = new ManualResetEvent(false);
				var transport = (RhinoQueuesTransport)container.Resolve<ITransport>();
            	transport.MessageProcessingCompleted += (information, exception) => wait.Set();
				bus.Send(bus.Endpoint, "foobar");
            	wait.WaitOne();
            }

			using (var container = CreateContainer())
			using (var bus = container.Resolve<IStartableServiceBus>())
			{
				bus.Start();
			}
        }
    }
}
