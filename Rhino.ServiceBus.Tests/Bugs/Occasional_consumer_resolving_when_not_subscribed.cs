using System;
using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
	public class Occasional_consumer_resolving_when_not_subscribed : OccasionalConsumerOf<SimpleMessage>
	{
		private readonly IWindsorContainer container;
    	public static ManualResetEvent wait;
		public static bool GotConsumed;

		public Occasional_consumer_resolving_when_not_subscribed()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
			container.AddComponent<Occasional_consumer_resolving_when_not_subscribed>();
			container.AddComponent<Not_consumed>();
        }

		[Fact]
		public void Would_not_gather_occasional_consumer_if_not_instance_subscribed()
		{
			using (var bus = container.Resolve<IStartableServiceBus>())
			{
				wait = new ManualResetEvent(false);
				bus.Start();
				using (bus.AddInstanceSubscription(this))
				{
					bus.Send(new SimpleMessage());
					wait.WaitOne(TimeSpan.FromSeconds(5), false);
				}
			}
			Assert.False(GotConsumed);
		}

		public void Consume(SimpleMessage message)
		{
		}
	}

	public class SimpleMessage { }

	public class Not_consumed : OccasionalConsumerOf<SimpleMessage>
	{
		public void Consume(SimpleMessage message)
		{
			Occasional_consumer_resolving_when_not_subscribed.GotConsumed = true;
			Occasional_consumer_resolving_when_not_subscribed.wait.Set();
		}
	}

	
}
