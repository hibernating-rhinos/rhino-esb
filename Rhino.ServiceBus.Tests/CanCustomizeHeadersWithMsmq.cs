using System;
using System.Collections.Specialized;
using System.Messaging;
using System.Threading;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Util;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class CanCustomizeHeadersWithMsmq : MsmqTestBase
    {
        [Fact]
        public void it_should_add_custom_header_to_headers_collection_for_normal_messages()
        {
            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<ICustomizeMessageHeaders>().ImplementedBy<AppIdentityCustomizer>().LifeStyle.Is(LifestyleType.Transient));
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
                container.Register(Component.For<ICustomizeMessageHeaders>().ImplementedBy<AppIdentityCustomizer>().LifeStyle.Is(LifestyleType.Transient));
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

        public class AppIdentityCustomizer : ICustomizeMessageHeaders
        {
            public void Customize(NameValueCollection headers)
            {
                headers.Add("user-id", "corey");
            }
        }
    }
}