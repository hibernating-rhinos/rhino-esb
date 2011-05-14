using System;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.MessageModules;
using StructureMap;
using Xunit;

namespace Rhino.ServiceBus.Tests.Containers.StructureMap
{
    public class ContainerTests
    {
        [Fact]
        public void Consumer_must_be_transient()
        {
            var container = ObjectFactory.Container;
            container.Configure(c => c.For<TestConsumer>().Singleton().Use<TestConsumer>());
            new RhinoServiceBusConfiguration()
                .UseStructureMap(container)
                .Configure();

            try
            {
                container.GetInstance<TestConsumer>();
            }
            catch (Exception ex)
            {
                Assert.True(null != ex.InnerException as InvalidUsageException);
            }
        }

        [Fact]
        public void Bus_instance_is_singleton()
        {
            var container = ObjectFactory.Container;
            new RhinoServiceBusConfiguration()
                .UseStructureMap(container)
                .Configure();

            var startable = container.GetInstance<IStartableServiceBus>();
            var bus = container.GetInstance<IServiceBus>();
            Assert.Same(startable, bus);
        }

        [Fact]
        public void Oneway_bus_is_singleton()
        {
            var container = ObjectFactory.Container;
            new OnewayRhinoServiceBusConfiguration()
                .UseStructureMap(container)
                .Configure();

            var oneWayBus = container.GetInstance<IOnewayBus>();
            var second = container.GetInstance<IOnewayBus>();
            Assert.Same(oneWayBus, second);
        }

        [Fact]
        public void RhinoQueues_bus_is_registered()
        {
            var container = new Container();
            new RhinoServiceBusConfiguration()
                .UseStructureMap(container)
                .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
                .Configure();

            var bus = container.GetInstance<IServiceBus>();
            Assert.NotNull(bus);
        }

        [Fact]
        public void LoadBalancer_is_singleton()
        {
            var container = new Container();
            new LoadBalancerConfiguration()
                .UseStructureMap(container)
                .UseStandaloneConfigurationFile("LoadBalancer.config")
                .Configure();

            var startable = container.GetInstance<IStartable>();
            var loadBalancer = container.GetInstance<MsmqLoadBalancer>();
            Assert.Same(startable, loadBalancer);
        }

        [Fact]
        public void Registers_logging_module()
        {
            var container = new Container();
            new RhinoServiceBusConfiguration()
                .UseStructureMap(container)
                .UseStandaloneConfigurationFile("BusWithLogging.config")
                .Configure();

            var loggingModule = container.GetInstance<MessageLoggingModule>();
            Assert.NotNull(loggingModule);
        }

        [Fact]
        public void Registers_load_balancer_module()
        {
            var container = new Container();
            new RhinoServiceBusConfiguration()
                .UseStructureMap(container)
                .UseStandaloneConfigurationFile("LoadBalancer/BusWithLoadBalancer.config")
                .Configure();

            var loadBalancerMessageModule = container.GetInstance<LoadBalancerMessageModule>();
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