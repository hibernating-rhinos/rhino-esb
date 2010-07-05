using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.Queues;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.RhinoQueues;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class CanCustomizeMessageConstruction:IDisposable
    {
        private WindsorContainer container;

        public CanCustomizeMessageConstruction()
        {
            container = new WindsorContainer("RhinoQueues/RhinoQueues.config");

            container.Register(Component.For<IMessageBuilder<MessagePayload>>().ImplementedBy<CustomHeaderBuilder>());
            container.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            

        }


        [Fact]
        public void it_should_add_custom_header_to_headers_collection()
        {
            var builder = container.Resolve<IMessageBuilder<MessagePayload>>();
            builder.Initialize(new Endpoint {Uri = RhinoQueuesOneWayBus.NullEndpoint});
            var msg = builder.BuildFromMessageBatch("somemsg");
            Assert.NotNull(msg);
            Assert.NotEqual(0,msg.Data.Length);
            Assert.Equal("mikey",msg.Headers["user-id"]);

        }
        
        public class CustomHeaderBuilder : IMessageBuilder<MessagePayload>
        {
            private IMessageBuilder<MessagePayload> inner;

            public CustomHeaderBuilder(IMessageBuilder<MessagePayload> inner)
            {
                this.inner = inner;
            }

            public MessagePayload BuildFromMessageBatch(params object[] msgs)
            {
                var payload = inner.BuildFromMessageBatch(msgs);
                Contextualize(payload);
                return payload;
            }

            public void Initialize(Endpoint source)
            {
                inner.Initialize(source);
            }

            private static void Contextualize(MessagePayload message)
            {
                message.Headers.Add("user-id","mikey");
            }
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}