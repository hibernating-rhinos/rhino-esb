using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Xunit;
using System.Linq;

namespace Rhino.ServiceBus.Tests.Hosting
{
    public class Can_host_in_another_app_domain : MsmqTestBase, IDisposable, OccasionalConsumerOf<string>
    {
        readonly RemoteAppDomainHost host = new RemoteAppDomainHost(typeof(TestBootStrapper));
        private string reply;

        private readonly ManualResetEvent resetEvent = new ManualResetEvent(false);

        private readonly IWindsorContainer container;

        public Can_host_in_another_app_domain()
        {
            container = new WindsorContainer(new XmlInterpreter(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnotherBus.config")
                ));
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
        }

		[Fact]
		public void Components_are_registered_using_name_only()
		{
			var windsorContainer = new WindsorContainer();
			new SimpleBootStrapper().InitializeContainer(windsorContainer);
			var handler = windsorContainer.Kernel.GetHandler(typeof(TestRemoteHandler).Name);
			Assert.NotNull(handler);
		}

        [Fact]
        public void And_accept_messages_from_there()
        {
            host.Start();

            using(var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                using(bus.AddInstanceSubscription(this))
                {
                    bus.Send(new Uri("msmq://localhost/test_queue").ToEndpoint(), "hello");

                    resetEvent.WaitOne(TimeSpan.FromSeconds(30));

                    Assert.Equal("olleh", reply);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            host.Close();
        }

        public void Consume(string message)
        {
            reply = message;
            resetEvent.Set();
        }
    }

	public class SimpleBootStrapper : AbstractBootStrapper
	{
		
	}

    public class TestBootStrapper : AbstractBootStrapper
    {
        protected override void ConfigureContainer()
        {
            container.AddComponent<TestRemoteHandler>();
        }
    }

    public class TestRemoteHandler : ConsumerOf<string>
    {
        private readonly IServiceBus bus;

        public TestRemoteHandler(IServiceBus bus)
        {
            this.bus = bus;
        }

        public void Consume(string message)
        {
            bus.Reply(new String(message.Reverse().ToArray()));
        }
    }
}