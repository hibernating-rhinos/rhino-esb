using System;
using System.Linq;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Unity
{
    public class UnityBuilder : IBusContainerBuilder
    {
        private readonly IUnityContainer container;
        private readonly AbstractRhinoServiceBusConfiguration configuration;

        public UnityBuilder(IUnityContainer container, AbstractRhinoServiceBusConfiguration configuration)
        {
            this.container = container;
            this.configuration = configuration;
            this.configuration.BuildWith(this);
        }

        public void WithInterceptor(IConsumerInterceptor interceptor)
        {
            container.AddExtension(new ConsumerExtension(interceptor));
        }

        public void RegisterDefaultServices()
        {
            if (!container.IsRegistered(typeof(IUnityContainer)))
                container.RegisterInstance(container);

            container.RegisterType<IServiceLocator, UnityServiceLocator>();

            var types = typeof (IServiceBus).Assembly.GetTypes().Where(t => typeof (IBusConfigurationAware).IsAssignableFrom(t)).ToList();
            types.ForEach(t => container.RegisterType(typeof (IBusConfigurationAware), t, new ContainerControlledLifetimeManager()));
            
            foreach (var configurationAware in container.ResolveAll<IBusConfigurationAware>())
            {
                configurationAware.Configure(configuration, this);
            }

            foreach (var type in configuration.MessageModules)
            {
                if (container.IsRegistered(type) == false)
                    container.RegisterType(type, type.FullName);
            }

            container.RegisterType<IReflection, DefaultReflection>(new ContainerControlledLifetimeManager());
            container.RegisterType(typeof(IMessageSerializer), configuration.SerializerType, new ContainerControlledLifetimeManager());
            container.RegisterType<IEndpointRouter, EndpointRouter>(new ContainerControlledLifetimeManager());
        }

        public void RegisterBus()
        {
            throw new NotImplementedException();
        }

        public void RegisterPrimaryLoadBalancer()
        {
            throw new NotImplementedException();
        }

        public void RegisterSecondaryLoadBalancer()
        {
            throw new NotImplementedException();
        }

        public void RegisterReadyForWork()
        {
            throw new NotImplementedException();
        }

        public void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint)
        {
            throw new NotImplementedException();
        }

        public void RegisterLoggingEndpoint(Uri logEndpoint)
        {
            throw new NotImplementedException();
        }

        public void RegisterMsmqTransport(Type queueStrategyType)
        {
            throw new NotImplementedException();
        }

        public void RegisterQueueCreation()
        {
            throw new NotImplementedException();
        }

        public void RegisterMsmqOneWay()
        {
            throw new NotImplementedException();
        }

        public void RegisterRhinoQueuesTransport(string path, string name)
        {
            throw new NotImplementedException();
        }

        public void RegisterRhinoQueuesOneWay()
        {
            throw new NotImplementedException();
        }

        public void RegisterSecurity(byte[] key)
        {
            throw new NotImplementedException();
        }

        public void RegisterNoSecurity()
        {
            throw new NotImplementedException();
        }
    }
}