using System;
using System.Collections.Specialized;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.Queues;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.RhinoQueues;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class CustomizingMessageConstruction
    {
        
        [Fact]
        public void it_should_add_custom_header_to_headers_collection_using_builder()
        {
            using( var container = new WindsorContainer("RhinoQueues/RhinoQueues.config"))
            {
                container.Register(Component.For<IMessageBuilder<MessagePayload>>().ImplementedBy<CustomHeaderBuilder>());//before facility
                container.AddFacility("rhino.esb", new RhinoServiceBusFacility());

                var builder = container.Resolve<IMessageBuilder<MessagePayload>>();
                builder.Initialize(new Endpoint { Uri = RhinoQueuesOneWayBus.NullEndpoint });
                var msg = builder.BuildFromMessageBatch("somemsg");
                Assert.NotNull(msg);
                Assert.NotEqual(0, msg.Data.Length);
                Assert.Equal("mikey", msg.Headers["user-id"]);    
            }

        }

        [Fact]
        public void it_should_add_custom_header_to_headers_collection_using_interface()
        {
            using (var container = new WindsorContainer("RhinoQueues/RhinoQueues.config"))
            {
                container.AddFacility("rhino.esb", new RhinoServiceBusFacility());
                container.Register(Component.For<ICustomizeMessageHeaders>().ImplementedBy<AppIdentityCustomizer>().LifeStyle.Is(LifestyleType.Transient));

                var builder = container.Resolve<IMessageBuilder<MessagePayload>>();
                builder.Initialize(new Endpoint { Uri = RhinoQueuesOneWayBus.NullEndpoint });
                var msg = builder.BuildFromMessageBatch("somemsg");
                Assert.NotNull(msg);
                Assert.NotEqual(0, msg.Data.Length);
                Assert.Equal("mikey", msg.Headers["user-id"]);
            }

        }
        
        [CLSCompliant(false)]
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
        public class AppIdentityCustomizer : ICustomizeMessageHeaders
        {
            public void Customize(NameValueCollection headers)
            {
                headers.Add("user-id","mikey");
            }
        }

    }

    
}