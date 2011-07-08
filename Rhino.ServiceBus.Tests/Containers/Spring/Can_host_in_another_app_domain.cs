using System;
using System.IO;
using System.Linq;
using System.Threading;

using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Spring;

using Spring.Context;
using Spring.Context.Support;

using Xunit;

namespace Rhino.ServiceBus.Tests.Containers.Spring
{
    public class Can_host_in_another_app_domain : MsmqTestBase, OccasionalConsumerOf<StringMsg>
    {
        private readonly RemoteAppDomainHost host = new RemoteAppDomainHost(
            Path.Combine(Environment.CurrentDirectory, "Rhino.ServiceBus.Tests.dll"), typeof (TestBootStrapper));

        private string reply;

        private readonly ManualResetEvent resetEvent = new ManualResetEvent(false);

        private readonly IConfigurableApplicationContext applicationContext;

        public Can_host_in_another_app_domain()
        {
            applicationContext = new StaticApplicationContext();
            new RhinoServiceBusConfiguration()
                .UseSpring(applicationContext)
                .UseStandaloneConfigurationFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnotherBus.config"))
                .Configure();
        }

        [Fact]
        public void And_accept_messages_from_there()
        {
            host.Start();

            using (var bus = applicationContext.Get<IStartableServiceBus>())
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
    public class SimpleBootStrapper : SpringBootStrapper
    {
        public SimpleBootStrapper(IConfigurableApplicationContext container)
            : base(container)
        {
        }
    }

    [CLSCompliant(false)]
    public class TestBootStrapper : SpringBootStrapper
    {
        protected override void ConfigureContainer()
        {
            ApplicationContext.RegisterPrototype<TestRemoteHandler>();
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