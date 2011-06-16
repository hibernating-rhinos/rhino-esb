
using System;
using System.Linq;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests.Containers.Unity
{
    public class ContainerTests
    {
        [Fact]
        public void Consumer_must_be_transient()
        {
            var container = new UnityContainer();
            container.RegisterType<TestConsumer>(new ContainerControlledLifetimeManager());
            new RhinoServiceBusConfiguration()
                .UseUnity(container)
                .Configure();

            try
            {
                container.Resolve<TestConsumer>();
            }
            catch (Exception ex)
            {
                Assert.True(null != ex.InnerException as InvalidUsageException);
            }
        }

        [Fact]
        public void Bus_instance_is_singleton()
        {
            var container = new UnityContainer();
            new RhinoServiceBusConfiguration()
                .UseUnity(container)
                .Configure();

            var startable = container.Resolve<IStartableServiceBus>();
            var bus = container.Resolve<IServiceBus>();
            Assert.Same(startable, bus);
        }
        
        [Fact]
        public void Oneway_bus_is_singleton()
        {
            var container = new UnityContainer();
            new OnewayRhinoServiceBusConfiguration()
                .UseUnity(container)
                .Configure();

            var oneWayBus = container.Resolve<IOnewayBus>();
            var second = container.Resolve<IOnewayBus>();
            Assert.Same(oneWayBus, second);
        }

        [Fact]
        public void RhinoQueues_bus_is_registered()
        {
            var container = new UnityContainer();
            new RhinoServiceBusConfiguration()
                .UseUnity(container)
                .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
                .Configure();

            var bus = container.Resolve<IServiceBus>();
            Assert.NotNull(bus);
        }

        [Fact]
        public void LoadBalancer_is_singleton()
        {
            var container = new UnityContainer();
            new LoadBalancerConfiguration()
                .UseUnity(container)
                .UseStandaloneConfigurationFile("LoadBalancer.config")
                .Configure();

            var startable = container.Resolve<IStartable>();
            var loadBalancer = container.Resolve<MsmqLoadBalancer>();
            Assert.Same(startable, loadBalancer);
        }

        [Fact]
        public void Registers_logging_module()
        {
            var container = new UnityContainer();
            new RhinoServiceBusConfiguration()
                .UseUnity(container)
                .UseStandaloneConfigurationFile("BusWithLogging.config")
                .Configure();

            var loggingModule = container.Resolve<MessageLoggingModule>(typeof(MessageLoggingModule).FullName);
            Assert.NotNull(loggingModule);
        }

        [Fact]
        public void Registers_load_balancer_module()
        {
            var container = new UnityContainer();
            new RhinoServiceBusConfiguration()
                .UseUnity(container)
                .UseStandaloneConfigurationFile("LoadBalancer/BusWithLoadBalancer.config")
                .Configure();

            var loadBalancerMessageModule = container.Resolve<LoadBalancerMessageModule>(typeof(LoadBalancerMessageModule).FullName);
            Assert.NotNull(loadBalancerMessageModule);
        }

        [Fact]
        public void QueueCreationModule_can_be_resolved()
        {
            var container = new UnityContainer();
            new RhinoServiceBusConfiguration()
                .UseUnity(container)
                .Configure();

            var allBusAware = container.ResolveAll<IServiceBusAware>().ToList();
            Assert.NotEmpty(allBusAware);
            Assert.IsType<QueueCreationModule>(allBusAware.First());
        }

        [Fact]
        public void DeploymentActions_can_be_resolved()
        {
            var container = new UnityContainer();
            new RhinoServiceBusConfiguration()
                .UseUnity(container)
                .UseStandaloneConfigurationFile("BusWithLogging.config")
                .Configure();

            var actions = container.ResolveAll<IDeploymentAction>().ToList();
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