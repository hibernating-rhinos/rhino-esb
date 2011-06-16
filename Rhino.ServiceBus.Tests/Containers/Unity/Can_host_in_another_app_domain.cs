using System;
using System.IO;
using System.Threading;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Unity;
using Xunit;
using System.Linq;

namespace Rhino.ServiceBus.Tests.Containers.Unity
{
    public class Can_host_in_another_app_domain : MsmqTestBase, OccasionalConsumerOf<StringMsg>
    {
        readonly RemoteAppDomainHost host = new RemoteAppDomainHost(
            Path.Combine(Environment.CurrentDirectory, "Rhino.ServiceBus.Tests.dll"), typeof(TestBootStrapper));
        private string reply;

        private readonly ManualResetEvent resetEvent = new ManualResetEvent(false);

        private readonly IUnityContainer container;

        public Can_host_in_another_app_domain()
        {
            container = new UnityContainer();
            new RhinoServiceBusConfiguration()
                .UseUnity(container)
                .UseStandaloneConfigurationFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnotherBus.config"))
                .Configure();
        }

        [Fact]
        public void And_accept_messages_from_there()
        {
            host.Start();

            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                using (bus.AddInstanceSubscription(this))
                {
                    bus.Send(new Uri("msmq://localhost/test_queue").ToEndpoint(), new StringMsg
                    {
                        Value = "hello"
                    });

                    Assert.True(resetEvent.WaitOne(TimeSpan.FromSeconds(10), false));

                    Assert.Equal("olleh", reply);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            host.Close();
        }

        public void Consume(StringMsg message)
        {
            reply = message.Value;
            resetEvent.Set();
        }
    }

    [CLSCompliant(false)]
    public class SimpleBootStrapper : UnityBootStrapper
    {
        public SimpleBootStrapper(IUnityContainer container)
            : base(container)
        {
        }
    }

    [CLSCompliant(false)]
    public class TestBootStrapper : UnityBootStrapper
    {
        protected override void ConfigureContainer()
        {
            Container.RegisterType<TestRemoteHandler>();
        }
    }

    public class TestRemoteHandler : ConsumerOf<StringMsg>
    {
        private readonly IServiceBus bus;

        public TestRemoteHandler(IServiceBus bus)
        {
            this.bus = bus;
        }

        public void Consume(StringMsg message)
        {
            bus.Reply(new StringMsg
            {
                Value = new String(message.Value.Reverse().ToArray())
            });
        }
    }

    public class StringMsg
    {
        public string Value { get; set; }
    }
}