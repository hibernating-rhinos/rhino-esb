using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class When_handler_consumes_two_messages : MsmqTestBase
    {
        private readonly IWindsorContainer container;

        public When_handler_consumes_two_messages()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
        }

        [Fact]
        public void Should_be_registered()
        {
            container.AddComponent<MyHandler>();

            var bus = (DefaultServiceBus)container.Resolve<IServiceBus>();
            var consumers = bus.GatherConsumers(new CurrentMessageInformation
            {
                Message =  5
            });

            Assert.NotEmpty(consumers);
        }

        [Fact]
        public void Should_be_able_to_register_using_cotnainer_AllTypesOf()
        {
            container.Register(
                AllTypes.FromAssemblyContaining<MyHandler>()
                    .Where(x => x == typeof(MyHandler))
                );

            var bus = (DefaultServiceBus)container.Resolve<IServiceBus>();
            var consumers = bus.GatherConsumers(new CurrentMessageInformation
            {
                Message = 5
            });

            Assert.NotEmpty(consumers);
        }

        [Fact]
        public void Should_be_registered_for_second_type()
        {
            container.AddComponent<MyHandler>();

            var bus = (DefaultServiceBus)container.Resolve<IServiceBus>();
            var consumers = bus.GatherConsumers(new CurrentMessageInformation
            {
                Message = "foo"
            });

            Assert.NotEmpty(consumers);
        }

        [Fact]
        public void Should_be_able_to_register_using_cotnainer_AllTypesOf_for_second_type()
        {
            container.Register(
                AllTypes.FromAssemblyContaining<MyHandler>()
                    .Where(x => x.Name == "MyHandler")
                );

            var bus = (DefaultServiceBus)container.Resolve<IServiceBus>();
            var consumers = bus.GatherConsumers(new CurrentMessageInformation
            {
                Message = "bar"
            });

            Assert.NotEmpty(consumers);
        }


        public class MyHandler : ConsumerOf<int>, ConsumerOf<string>
        {
            public void Consume(int message)
            {
                
            }

            public void Consume(string message)
            {
            }
        }
    }
}