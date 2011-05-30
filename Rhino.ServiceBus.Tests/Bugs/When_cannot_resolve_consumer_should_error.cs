using Castle.MicroKernel.Handlers;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;

namespace Rhino.ServiceBus.Tests.Bugs
{
    using System;
    using Impl;
    using Xunit;

    public class When_cannot_resolve_consumer_because_of_missing_dependecies : MsmqTestBase
    {
        private readonly IWindsorContainer container;

        public When_cannot_resolve_consumer_because_of_missing_dependecies()
        {
            container = new WindsorContainer(new XmlInterpreter());
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .Configure();
        }

        [Fact]
        public void Should_result_in_error()
        {
            container.Register(Component.For<MyHandler2>());

            var bus = (DefaultServiceBus)container.Resolve<IServiceBus>();
            Assert.Throws<HandlerException>(() =>
            {
                bus.GatherConsumers(new CurrentMessageInformation
                {
                    Message = DateTime.Now
                });

            });
        }

        public class MyHandler2 : ConsumerOf<DateTime>
        {
            private readonly int port;

            public MyHandler2(int port)
            {
                this.port = port;
            }

            public void Consume(DateTime m)
            {
                Console.WriteLine(port);
            }
        }
    }
}