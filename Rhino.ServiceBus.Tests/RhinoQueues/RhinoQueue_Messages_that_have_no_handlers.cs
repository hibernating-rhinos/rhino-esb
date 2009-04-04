using System;
using System.IO;
using System.Messaging;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.RhinoQueues;
using Rhino.ServiceBus.Tests.RhinoQueues;
using Rhino.ServiceBus.Transport;
using Xunit;
using Rhino.Queues;
using System.Net;
using System.Threading;

namespace Rhino.ServiceBus.Tests
{
	public class RhinoQueues_Messages_that_have_no_handlers : WithDebugging, IDisposable
    {
        private readonly IWindsorContainer container;

		public RhinoQueues_Messages_that_have_no_handlers()
        {
            if(Directory.Exists("test.esent"))
                Directory.Delete("test.esent", true);

            if (Directory.Exists("test_subscriptions.esent"))
                Directory.Delete("test_subscriptions.esent", true);

            container = new WindsorContainer(new XmlInterpreter("RhinoQueues/RhinoQueues.config"));
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
        }


        [Fact]
        public void Should_go_to_discard_sub_queue()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
                bus.Send(bus.Endpoint, "foobar");
            	var transport = (RhinoQueuesTransport) container.Resolve<ITransport>();
				Thread.Sleep(2000);
				//in RhinoQueuesTransport.Start() the TimeoutAction is created and threw NullReferenceException
				//if the queue had discarded message due to no handlers
            	var timeoutAction = new TimeoutAction(transport.Queue);
            }
        }

		public void Dispose()
		{
			container.Dispose();
		}
    }
}
