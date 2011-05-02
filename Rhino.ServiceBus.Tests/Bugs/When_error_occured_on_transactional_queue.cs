using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Transport;

namespace Rhino.ServiceBus.Tests.Bugs
{
    using System;
    using System.Messaging;
    using Impl;
    using Msmq;
    using Xunit;

    public class When_error_occured_on_transactional_queue : MsmqTestBase
    {

        private readonly IWindsorContainer container;

        public When_error_occured_on_transactional_queue()
        {
            container = new WindsorContainer();
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .UseStandaloneConfigurationFile("BusOnTransactionalQueue.config")
                .Configure();
            container.Register(Component.For<ThrowingConsumer>());
        }

        [Fact]
        public void Error_subqeueue_will_contain_error_details()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                bus.Send(bus.Endpoint, DateTime.Now);

                using (var q = MsmqUtil.GetQueuePath(bus.Endpoint).Open())
                using (var errorSubQueue = q.OpenSubQueue(SubQueue.Errors, QueueAccessMode.SendAndReceive))
                {
                    var originalMessage = errorSubQueue.Receive();
                    var errorDescripotion = errorSubQueue.Receive();
                    Assert.Equal("Error description for: " + originalMessage.Label, errorDescripotion.Label);
                }
            }
        }

        public class ThrowingConsumer : ConsumerOf<DateTime>
        {
            public void Consume(DateTime message)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}