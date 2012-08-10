using System;
using System.Messaging;
using System.Threading;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Util;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class CanCustomizeOutgoingMessagesWithMsmq : MsmqTestBase
    {
        [Fact]
        public void it_should_add_custom_header_to_headers_collection_for_normal_messages()
        {
            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<ICustomizeOutgoingMessages>().ImplementedBy<AppIdentityCustomizer>().LifeStyle.Is(LifestyleType.Transient));
                new RhinoServiceBusConfiguration()
                    .UseCastleWindsor(container)
                    .Configure();
                var builder = container.Resolve<IMessageBuilder<Message>>();
                Message afterBuild = null;
                builder.MessageBuilt += msg => afterBuild = msg;

                using (var bus = container.Resolve<IStartableServiceBus>())
                {
                    bus.Start();
                    bus.Send(bus.Endpoint, "testmessage");
                }
                Assert.NotNull(afterBuild);
                var headers = afterBuild.Extension.DeserializeHeaders();
                Assert.Equal("corey", headers["user-id"]);
            }
        }

        [Fact]
        public void it_should_add_custom_header_to_headers_collection_for_delayed_messages()
        {
            using (var container = new WindsorContainer(new XmlInterpreter()))
            {
                container.Register(Component.For<ICustomizeOutgoingMessages>().ImplementedBy<AppIdentityCustomizer>().LifeStyle.Is(LifestyleType.Transient));
                new RhinoServiceBusConfiguration()
                    .UseCastleWindsor(container)
                    .Configure();
                var transport = container.Resolve<ITransport>();
                MsmqCurrentMessageInformation currentMessageInformation = null;
                var waitHandle = new ManualResetEvent(false);
                transport.MessageArrived += messageInfo =>
                {
                    currentMessageInformation = (MsmqCurrentMessageInformation) messageInfo;
                    waitHandle.Set();
                    return true;
                };
                var builder = container.Resolve<IMessageBuilder<Message>>();
                Message afterBuild = null;
                builder.MessageBuilt += msg => afterBuild = msg;

                using (var bus = container.Resolve<IStartableServiceBus>())
                {
                    bus.Start();
                    DateTime beforeSend = DateTime.Now;
                    bus.DelaySend(bus.Endpoint, DateTime.Now.AddMilliseconds(250), "testmessage");
                    waitHandle.WaitOne(TimeSpan.FromSeconds(30));
                    Assert.True((DateTime.Now - beforeSend).TotalMilliseconds >= 250);
                }
                Assert.NotNull(afterBuild);
                var headers = afterBuild.Extension.DeserializeHeaders();
                Assert.Equal("corey", headers["user-id"]);
                Assert.Equal("corey", currentMessageInformation.Headers["user-id"]);
            }
        }

        [Fact]
        public void TimeToReachQueue_is_set_when_DeliverBy_is_specified()
        {
            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<ICustomizeOutgoingMessages>()
                    .ImplementedBy<DeliverByCustomizer>()
                    .LifeStyle.Is(LifestyleType.Transient));
                new RhinoServiceBusConfiguration()
                    .UseCastleWindsor(container)
                    .Configure();
                var builder = container.Resolve<IMessageBuilder<Message>>();
                Message afterBuild = null;
                builder.MessageBuilt += msg => afterBuild = msg;

                using (var bus = container.Resolve<IStartableServiceBus>())
                {
                    bus.Start();
                    bus.Send(bus.Endpoint, "testmessage");
                }

                Assert.NotNull(afterBuild);
                Assert.NotEqual(Message.InfiniteTimeout, afterBuild.TimeToReachQueue);
            }
        }

        [Fact]
        public void TimeToReachQueue_is_set_when_MaxAttempts_is_one()
        {
            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<ICustomizeOutgoingMessages>()
                    .ImplementedBy<MaxAttemptCustomizer>()
                    .LifeStyle.Is(LifestyleType.Transient));
                new RhinoServiceBusConfiguration()
                    .UseCastleWindsor(container)
                    .Configure();
                var builder = container.Resolve<IMessageBuilder<Message>>();
                Message afterBuild = null;
                builder.MessageBuilt += msg => afterBuild = msg;

                using (var bus = container.Resolve<IStartableServiceBus>())
                {
                    bus.Start();
                    bus.Send(bus.Endpoint, "testmessage");
                }

                Assert.NotNull(afterBuild);
                Assert.Equal(TimeSpan.Zero, afterBuild.TimeToReachQueue);
            }
        }

        [Fact]
        public void Throws_when_MaxAttempts_is_greater_than_one()
        {
            using (var container = new WindsorContainer())
            {
                MaxAttemptCustomizer.MaxAttempts = 2;
                container.Register(Component.For<ICustomizeOutgoingMessages>()
                    .ImplementedBy<MaxAttemptCustomizer>()
                    .LifeStyle.Is(LifestyleType.Transient));
                new RhinoServiceBusConfiguration()
                    .UseCastleWindsor(container)
                    .Configure();
                var builder = container.Resolve<IMessageBuilder<Message>>();
                Message afterBuild = null;
                builder.MessageBuilt += msg => afterBuild = msg;

                using (var bus = container.Resolve<IStartableServiceBus>())
                {
                    bus.Start();

                    Assert.Throws<InvalidUsageException>(() =>
                    {
                        bus.Send(bus.Endpoint, "testmessage");
                    });
                }
            }
        }

        public class AppIdentityCustomizer : ICustomizeOutgoingMessages
        {
            public void Customize(OutgoingMessageInformation messageInformation)
            {
                messageInformation.Headers.Add("user-id", "corey");
            }
        }

        public class DeliverByCustomizer : ICustomizeOutgoingMessages
        {
            public void Customize(OutgoingMessageInformation messageInformation)
            {
                messageInformation.DeliverBy = DateTime.Now.AddMinutes(1);
            }
        }

        public class MaxAttemptCustomizer : ICustomizeOutgoingMessages
        {
            public static int MaxAttempts = 1;
            public void Customize(OutgoingMessageInformation messageInformation)
            {
                messageInformation.MaxAttempts = MaxAttempts;
            }
        }
    }
}