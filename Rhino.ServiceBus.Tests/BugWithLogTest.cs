using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Messages;
using Xunit;
using System.Linq;

namespace Rhino.ServiceBus.Tests
{
    public class BugWithLogTest : MsmqTestBase
    {
        private readonly IWindsorContainer container;

        public BugWithLogTest()
        {
            container = new WindsorContainer(new XmlInterpreter("BusWithLogging.config"));
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
        }

        [Fact]
        public void LoggingModule_should_be_in_container()
        {
            Assert.True(container.Kernel.HasComponent(typeof(MessageLoggingModule)));
        }

        [Fact]
        public void When_sending_message_will_place_copy_in_log_queue()
        {
            using(var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                bus.Send(new TestMessage
                {
                    Email = "foo@bar.org",
                    Name = "ayende"
                });

                var serializer = container.Resolve<IMessageSerializer>();

                var msg = testQueue2.GetAllMessages()
                    .Select(x => serializer.Deserialize(x.BodyStream)[0])
                    .OfType<MessageSentMessage>()
                    .First();

                Assert.Equal(typeof(TestMessage).FullName, msg.MessageType);
                Assert.Equal("ayende", ((TestMessage)((object[])msg.Message)[0]).Name);
            }
        }

        public class TestMessage
        {
            public string Name { get; set; }
            public string Email { get; set; }
        }

        public class TestHandler : ConsumerOf<TestMessage>
        {
            public void Consume(TestMessage message)
            {
                
            }
        }
    }
}