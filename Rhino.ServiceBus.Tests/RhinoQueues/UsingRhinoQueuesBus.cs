using System;
using System.IO;
using System.Threading;
using System.Transactions;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class UsingRhinoQueuesBus : WithDebugging, IDisposable
    {
        private readonly IWindsorContainer container;
        private readonly IStartableServiceBus bus;

        public UsingRhinoQueuesBus()
        {
            if(Directory.Exists("test.esent"))
                Directory.Delete("test.esent", true);

            if (Directory.Exists("test_subscriptions.esent"))
                Directory.Delete("test_subscriptions.esent", true);

            StringConsumer.Value = null;
            StringConsumer.Wait = new ManualResetEvent(false);

            container = new WindsorContainer(new XmlInterpreter("RhinoQueues/RhinoQueues.config"));
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            container.AddComponent<StringConsumer>();
            bus = container.Resolve<IStartableServiceBus>();
            bus.Start();
        }

        [Fact]
        public void Can_send_and_receive_messages()
        {
            using(var tx = new TransactionScope())
            {
                bus.Send(bus.Endpoint, "hello");

                tx.Complete();
            }

            Assert.True(StringConsumer.Wait.WaitOne(TimeSpan.FromSeconds(100)));

            Assert.Equal("hello", StringConsumer.Value);
        }
        
        public void Dispose()
        {
            container.Dispose();   
        }

        public class StringConsumer : ConsumerOf<string>
        {
            public static string Value;
            public static ManualResetEvent Wait;

            public void Consume(string message)
            {
                Value = message;
                Wait.Set();
            }
        }
    }
}