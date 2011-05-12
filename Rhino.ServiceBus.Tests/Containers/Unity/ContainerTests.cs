
using System;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Unity;
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
        public void QueueStrategyCanBeResolved()
        {
            var container = new UnityContainer();
            new RhinoServiceBusConfiguration()
                .UseUnity(container)
                .Configure();
            var strategy = container.Resolve<IQueueStrategy>();
        }

        //[Fact]
        //public void Oneway_bus_is_singleton()
        //{
        //    var container = ObjectFactory.Container;
        //    new OnewayRhinoServiceBusConfiguration()
        //        .UseStructureMap(container)
        //        .Configure();

        //    var oneWayBus = container.GetInstance<IOnewayBus>();
        //    var second = container.GetInstance<IOnewayBus>();
        //    Assert.Same(oneWayBus, second);
        //}

        //[Fact]
        //public void RhinoQueues_bus_is_registered()
        //{
        //    var container = new Container();
        //    new RhinoServiceBusConfiguration()
        //        .UseStructureMap(container)
        //        .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
        //        .Configure();

        //    var bus = container.GetInstance<IServiceBus>();
        //    Assert.NotNull(bus);
        //}

        //[Fact]
        //public void LoadBalancer_is_singleton()
        //{
        //    var container = new Container();
        //    new LoadBalancerConfiguration()
        //        .UseStructureMap(container)
        //        .UseStandaloneConfigurationFile("LoadBalancer.config")
        //        .Configure();

        //    var startable = container.GetInstance<IStartable>();
        //    var loadBalancer = container.GetInstance<MsmqLoadBalancer>();
        //    Assert.Same(startable, loadBalancer);
        //}

        //[Fact]
        //public void Registers_logging_module()
        //{
        //    var container = new Container();
        //    new RhinoServiceBusConfiguration()
        //        .UseStructureMap(container)
        //        .UseStandaloneConfigurationFile("BusWithLogging.config")
        //        .Configure();

        //    var loggingModule = container.GetInstance<MessageLoggingModule>();
        //    Assert.NotNull(loggingModule);
        //}

        //[Fact]
        //public void Registers_load_balancer_module()
        //{
        //    var container = new Container();
        //    new RhinoServiceBusConfiguration()
        //        .UseStructureMap(container)
        //        .UseStandaloneConfigurationFile("LoadBalancer/BusWithLoadBalancer.config")
        //        .Configure();

        //    var loadBalancerMessageModule = container.GetInstance<LoadBalancerMessageModule>();
        //    Assert.NotNull(loadBalancerMessageModule);
        //}
    }
}