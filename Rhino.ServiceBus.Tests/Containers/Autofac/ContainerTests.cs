using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.MessageModules;
using Autofac;
using Xunit;

namespace Rhino.ServiceBus.Tests.Containers.Autofac
{
    public class ContainerTests
    {
        [Fact]
        public void Consumer_must_be_transient()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<TestConsumer>().AsSelf().SingleInstance();
            var container = containerBuilder.Build();
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .Configure();

            Assert.Throws<InvalidUsageException>(() => container.Resolve<TestConsumer>());
        }

        [Fact]
        public void Bus_instance_is_singleton()
        {
            var containerBuilder = new ContainerBuilder();
            var container = containerBuilder.Build();
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .Configure();

            var startable = container.Resolve<IStartableServiceBus>();
            var bus = container.Resolve<IServiceBus>();
            Assert.Same(startable, bus);
        }

        [Fact]
        public void Oneway_bus_is_singleton()
        {
            var containerBuilder = new ContainerBuilder();
            var container = containerBuilder.Build();
            new OnewayRhinoServiceBusConfiguration()
                .UseAutofac(container)
                .Configure();

            var oneWayBus = container.Resolve<IOnewayBus>();
            var second = container.Resolve<IOnewayBus>();
            Assert.Same(oneWayBus, second);
        }

        [Fact]
        public void RhinoQueues_bus_is_registered()
        {
            var containerBuilder = new ContainerBuilder();
            var container = containerBuilder.Build();
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
                .Configure();

            var bus = container.Resolve<IServiceBus>();
            Assert.NotNull(bus);
        }

        [Fact]
        public void LoadBalancer_is_singleton()
        {
            var containerBuilder = new ContainerBuilder();
            var container = containerBuilder.Build();
            new LoadBalancerConfiguration()
                .UseAutofac(container)
                .UseStandaloneConfigurationFile("LoadBalancer.config")
                .Configure();

            var startable = container.Resolve<IStartable>();
            var loadBalancer = container.Resolve<MsmqLoadBalancer>();
            Assert.Same(startable, loadBalancer);
        }

        [Fact]
        public void Registers_logging_module()
        {
            var containerBuilder = new ContainerBuilder();
            var container = containerBuilder.Build();
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .UseStandaloneConfigurationFile("BusWithLogging.config")
                .Configure();

            var loggingModule = container.Resolve<MessageLoggingModule>();
            Assert.NotNull(loggingModule);
        }

        [Fact]
        public void Registers_load_balancer_module()
        {
            var containerBuilder = new ContainerBuilder();
            var container = containerBuilder.Build();
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .UseStandaloneConfigurationFile("LoadBalancer/BusWithLoadBalancer.config")
                .Configure();

            var loadBalancerMessageModule = container.Resolve<LoadBalancerMessageModule>();
            Assert.NotNull(loadBalancerMessageModule);
        }
    }

    public class TestConsumer : ConsumerOf<string>
    {
        public void Consume(string message)
        {
            
        }
    }
}