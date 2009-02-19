using System;
using System.Messaging;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Msmq;
using Xunit;
using MessageType=Rhino.ServiceBus.Msmq.MessageType;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class Will_send_invalid_admin_message_to_error_queue   : MsmqTestBase
	{

		private readonly IWindsorContainer container;

		public Will_send_invalid_admin_message_to_error_queue()
		{
			container = new WindsorContainer(new XmlInterpreter());
			container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
		}

		[Fact]
		public void Error_subqeueue_will_contain_error_details()
		{
			using (var bus = container.Resolve<IStartableServiceBus>())
			{
				bus.Start();

                queue.Send(new Message
                {
                    AppSpecific =  (int)MessageType.AdministrativeMessageMarker,
                    Body = "as",
                    Label = "foo"
                });

				using (var q = MsmqUtil.GetQueuePath(bus.Endpoint).Open())
				using (var errorSubQueue = q.OpenSubQueue(SubQueue.Errors, QueueAccessMode.SendAndReceive))
				{
					var originalMessage = errorSubQueue.Receive();
					var errorDescripotion = errorSubQueue.Receive();
					Assert.Equal("Error description for: " + originalMessage.Label, errorDescripotion.Label);
				}
			}
		}

    }
}