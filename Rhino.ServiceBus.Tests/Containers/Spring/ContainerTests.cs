using System;
using System.Linq;

using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Spring;

using Spring.Context.Support;

using Xunit;

namespace Rhino.ServiceBus.Tests.Containers.Spring
{
    public class ContainerTests
    {
        [Fact]
        public void Consumer_must_be_transient()
        {
            StaticApplicationContext container = new StaticApplicationContext();
            container.RegisterPrototype<TestConsumer>();
            new RhinoServiceBusConfiguration()
                .UseSpring(container)
                .Configure();

            try
            {
                container.Get<TestConsumer>();
            }
            catch (Exception ex)
            {
                Assert.True(null != ex.InnerException as InvalidUsageException);
            }
        }

        [Fact]
        public void Bus_instance_is_singleton()
        {
            var container = new StaticApplicationContext();
            new RhinoServiceBusConfiguration()
                .UseSpring(container)
                .Configure();

            var startable = container.Get<IStartableServiceBus>();
            var bus = container.Get<IServiceBus>();
            Assert.Same(startable, bus);
        }

        [Fact]
        public void Oneway_bus_is_singleton()
        {
            var container = new StaticApplicationContext();
            new OnewayRhinoServiceBusConfiguration()
                .UseSpring(container)
                .Configure();

            var oneWayBus = container.Get<IOnewayBus>();
            var second = container.Get<IOnewayBus>();
            Assert.Same(oneWayBus, second);
        }

        [Fact]
        public void RhinoQueues_bus_is_registered()
        {
            var container = new StaticApplicationContext();
            new RhinoServiceBusConfiguration()
                .UseSpring(container)
                .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
                .Configure();

            var bus = container.Get<IServiceBus>();
            Assert.NotNull(bus);
        }

        [Fact]
        public void LoadBalancer_is_singleton()
        {
            var container = new StaticApplicationContext();
            new LoadBalancerConfiguration()
                .UseSpring(container)
                .UseStandaloneConfigurationFile("LoadBalancer.config")
                .Configure();

            var startable = container.Get<IStartable>();
            var loadBalancer = container.Get<MsmqLoadBalancer>();
            Assert.Same(startable, loadBalancer);
        }

        [Fact]
        public void Registers_logging_module()
        {
            var container = new StaticApplicationContext();
            new RhinoServiceBusConfiguration()
                .UseSpring(container)
                .UseStandaloneConfigurationFile("BusWithLogging.config")
                .Configure();

            var loggingModule = container.Get<MessageLoggingModule>(typeof (MessageLoggingModule).FullName);
            Assert.NotNull(loggingModule);
        }

        [Fact]
        public void Registers_load_balancer_module()
        {
            var container = new StaticApplicationContext();
            new RhinoServiceBusConfiguration()
                .UseSpring(container)
                .UseStandaloneConfigurationFile("LoadBalancer/BusWithLoadBalancer.config")
                .Configure();

            var loadBalancerMessageModule = container.Get<LoadBalancerMessageModule>(typeof (LoadBalancerMessageModule).FullName);
            Assert.NotNull(loadBalancerMessageModule);
        }

        [Fact]
        public void QueueCreationModule_can_be_resolved()
        {
            var container = new StaticApplicationContext();
            new RhinoServiceBusConfiguration()
                .UseSpring(container)
                .Configure();

            var allBusAware = container.GetAll<IServiceBusAware>().ToList();
            Assert.NotEmpty(allBusAware);
            Assert.IsType<QueueCreationModule>(allBusAware.First());
        }

        [Fact]
        public void DeploymentActions_can_be_resolved()
        {
            var container = new StaticApplicationContext();
            new RhinoServiceBusConfiguration()
                .UseSpring(container)
                .UseStandaloneConfigurationFile("BusWithLogging.config")
                .Configure();

            var actions = container.GetAll<IDeploymentAction>().ToList();
            Assert.True(actions.Count >= 2);
        }
    }

    public class TestConsumer : ConsumerOf<string>
    {
        public void Consume(string message)
        {
        }
    }
}