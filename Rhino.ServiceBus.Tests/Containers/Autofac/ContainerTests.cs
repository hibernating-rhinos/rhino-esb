using System;
using System.Linq;
using System.Messaging;
using Autofac.Core;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.MessageModules;
using Autofac;
using Rhino.ServiceBus.Msmq;
using Xunit;
using IStartable = Rhino.ServiceBus.Internal.IStartable;
using Rhino.ServiceBus.Actions;
using System.Collections.Generic;

namespace Rhino.ServiceBus.Tests.Containers.Autofac
{
    public class ContainerTests : IDisposable
    {
        private IContainer container;

        public ContainerTests()
        {
            var builder = new ContainerBuilder();
            container = builder.Build();
        }

        [Fact]
        public void Consumer_must_be_transient()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<TestConsumer>()
                .AsSelf()
                .SingleInstance();
            builder.Update(container);

            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .Configure();

            var resolutionException = Assert.Throws<DependencyResolutionException>(() => container.Resolve<TestConsumer>());

            Assert.NotNull(resolutionException.InnerException);
            Assert.IsType<InvalidUsageException>(resolutionException.InnerException);
        }

        [Fact]
        public void Bus_instance_is_singleton()
        {
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .Configure();

            var startable = container.Resolve<IStartableServiceBus>();
            var bus = container.Resolve<IServiceBus>();
            Assert.Same(startable, bus);
        }

        [Fact]
        public void ServiceLocator_can_be_resolved()
        {
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .Configure();

            var instance = container.Resolve<IServiceLocator>();

            Assert.NotNull(instance);
        }

        [Fact]
        public void Oneway_bus_is_singleton()
        {
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
            var queuePath = MsmqUtil.GetQueuePath(new Endpoint
            {
                Uri = new Uri("msmq://localhost/test.balancer")
            });
            var queueAcceptingPath = MsmqUtil.GetQueuePath(new Endpoint
            {
                Uri = new Uri("msmq://localhost/test.balancer.acceptingwork")
            });
            if (MessageQueue.Exists(queuePath.QueuePath) == false)
                MessageQueue.Create(queuePath.QueuePath);
            if (MessageQueue.Exists(queueAcceptingPath.QueuePath) == false)
                MessageQueue.Create(queueAcceptingPath.QueuePath);

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
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .UseStandaloneConfigurationFile("LoadBalancer/BusWithLoadBalancer.config")
                .Configure();

            var loadBalancerMessageModule = container.Resolve<LoadBalancerMessageModule>();
            Assert.NotNull(loadBalancerMessageModule);
        }

        [Fact]
        public void QueueCreationModule_can_be_resolved()
        {
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .Configure();

            var allBusAware = container.Resolve<IEnumerable<IServiceBusAware>>().ToList();
            Assert.NotEmpty(allBusAware);
            Assert.IsType<QueueCreationModule>(allBusAware.First());
        }

        [Fact]
        public void DeploymentActions_can_be_resolved()
        {
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .UseStandaloneConfigurationFile("BusWithLogging.config")
                .Configure();

            var actions = container.Resolve<IEnumerable<IDeploymentAction>>().ToList();
            Assert.True(actions.Count >= 2);
        }

        [Fact]
        public void Dispose_dose_not_throws()
        {
            new RhinoServiceBusConfiguration()
                .UseAutofac(container)
                .Configure();

            container.Dispose();
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }

    public class TestConsumer : ConsumerOf<string>
    {
        public void Consume(string message)
        {

        }
    }
}