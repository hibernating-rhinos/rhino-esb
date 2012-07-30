using System;
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
        public void CanCustomizeMessageBasedOnDestination()
        {
            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<ICustomizeOutgoingMessages>().ImplementedBy<CustomizeByDestination>());
                new RhinoServiceBusConfiguration()
                    .UseCastleWindsor(container)
                    .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
                    .Configure();

                var builder = container.Resolve<IMessageBuilder<MessagePayload>>();
                builder.Initialize(new Endpoint { Uri = RhinoQueuesOneWayBus.NullEndpoint });
                var messageInfo = new OutgoingMessageInformation
                {
                    Destination = new Endpoint { Uri = new Uri("null://nowhere/queue?Volatile=true") },
                    Messages = new[] { "somemsg" }
                };
                var msg = builder.BuildFromMessageBatch(messageInfo);
                Assert.NotNull(msg);
                Assert.NotEqual(0, msg.Data.Length);
                Assert.Equal(2, msg.MaxAttempts);
            }
        }

        [Fact]
        public void CanCustomizeMessageBasedMessageType()
        {
            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<ICustomizeOutgoingMessages>().ImplementedBy<CustomizeByMessageType>());
                new RhinoServiceBusConfiguration()
                    .UseCastleWindsor(container)
                    .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
                    .Configure();

                var builder = container.Resolve<IMessageBuilder<MessagePayload>>();
                builder.Initialize(new Endpoint { Uri = RhinoQueuesOneWayBus.NullEndpoint });
                var messageInfo = new OutgoingMessageInformation { Messages = new[] { new CustomizedMessage() } };
                var msg = builder.BuildFromMessageBatch(messageInfo);
                Assert.NotNull(msg);
                Assert.NotEqual(0, msg.Data.Length);
                Assert.Equal(1, msg.MaxAttempts);
            }
        }

        [Fact]
        public void it_should_add_custom_header_to_headers_collection_using_builder()
        {
            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<IMessageBuilder<MessagePayload>>().ImplementedBy<CustomHeaderBuilder>());//before configuration
                new RhinoServiceBusConfiguration()
                    .UseCastleWindsor(container)
                    .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
                    .Configure();

                var builder = container.Resolve<IMessageBuilder<MessagePayload>>();
                builder.Initialize(new Endpoint { Uri = RhinoQueuesOneWayBus.NullEndpoint });
                var messageInfo = new OutgoingMessageInformation { Messages = new[] { "somemsg" } };
                var msg = builder.BuildFromMessageBatch(messageInfo);
                Assert.NotNull(msg);
                Assert.NotEqual(0, msg.Data.Length);
                Assert.Equal("mikey", msg.Headers["user-id"]);
            }

        }

        [Fact]
        public void it_should_add_custom_header_to_headers_collection_using_interface()
        {
            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<ICustomizeOutgoingMessages>().ImplementedBy<AppIdentityCustomizer>().LifeStyle.Is(LifestyleType.Transient));
                new RhinoServiceBusConfiguration()
                    .UseCastleWindsor(container)
                    .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
                    .Configure();

                var builder = container.Resolve<IMessageBuilder<MessagePayload>>();
                builder.Initialize(new Endpoint { Uri = RhinoQueuesOneWayBus.NullEndpoint });
                var messageInfo = new OutgoingMessageInformation { Messages = new[] { "somemsg" } };
                var msg = builder.BuildFromMessageBatch(messageInfo);
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

            public event Action<MessagePayload> MessageBuilt;

            public MessagePayload BuildFromMessageBatch(OutgoingMessageInformation messageInformation)
            {
                var payload = inner.BuildFromMessageBatch(messageInformation);
                Contextualize(payload);

                if (MessageBuilt != null)
                    MessageBuilt(payload);
                return payload;
            }

            public void Initialize(Endpoint source)
            {
                inner.Initialize(source);
            }

            private static void Contextualize(MessagePayload message)
            {
                message.Headers.Add("user-id", "mikey");
            }
        }

        public class AppIdentityCustomizer : ICustomizeOutgoingMessages
        {
            public void Customize(OutgoingMessageInformation messageInformation)
            {
                messageInformation.Headers.Add("user-id", "mikey");
            }
        }

        public class CustomizeByDestination : ICustomizeOutgoingMessages
        {
            public void Customize(OutgoingMessageInformation messageInformation)
            {
                if (messageInformation.Destination != null
                    && messageInformation.Destination.Uri.Query.Contains("Volatile"))
                {
                    messageInformation.MaxAttempts = 2;
                }
            }
        }

        public class CustomizeByMessageType : ICustomizeOutgoingMessages
        {
            public void Customize(OutgoingMessageInformation messageInformation)
            {
                if (messageInformation.Messages[0] is ICustomizeMessageByType)
                {
                    messageInformation.MaxAttempts = 1;
                }
            }
        }

        public interface ICustomizeMessageByType
        {
        }

        public class CustomizedMessage : ICustomizeMessageByType
        {
        }
    }
}