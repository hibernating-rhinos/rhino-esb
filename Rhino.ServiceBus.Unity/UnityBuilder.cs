using System;
using System.Linq;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;

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

            typeof(IServiceBus).Assembly.GetTypes()
                .Where(t => typeof (IBusConfigurationAware).IsAssignableFrom(t)).ToList()
                .ForEach(type => container.RegisterType(typeof (IBusConfigurationAware), type, new ContainerControlledLifetimeManager()));
            
            foreach (var configurationAware in container.ResolveAll<IBusConfigurationAware>())
            {
                configurationAware.Configure(configuration, this);
            }

            foreach (var type in configuration.MessageModules)
            {
                if (container.IsRegistered(type) == false)
                    container.RegisterType(type, type.FullName);
            }

            container.RegisterType<IReflection, DefaultReflection>(new ContainerControlledLifetimeManager())
                .RegisterType(typeof(IMessageSerializer), configuration.SerializerType, new ContainerControlledLifetimeManager())
                .RegisterType<IEndpointRouter, EndpointRouter>(new ContainerControlledLifetimeManager());
        }

        public void RegisterBus()
        {
            var busConfig = (RhinoServiceBusConfiguration) configuration;

            container.RegisterType<IDeploymentAction, CreateLogQueueAction>()
                .RegisterType<IDeploymentAction, CreateQueuesAction>();

            container.RegisterType<DefaultServiceBus>(new ContainerControlledLifetimeManager())
                .RegisterType<IStartableServiceBus, DefaultServiceBus>(
                    new InjectionConstructor(
                        new ResolvedParameter<IServiceLocator>(),
                        new ResolvedParameter<ITransport>(),
                        new ResolvedParameter<ISubscriptionStorage>(),
                        new ResolvedParameter<IReflection>(),
                        new ResolvedArrayParameter<IMessageModule>(),
                        new InjectionParameter<MessageOwner[]>(busConfig.MessageOwners.ToArray()),
                        new ResolvedParameter<IEndpointRouter>()))
                .RegisterType<IServiceBus, DefaultServiceBus>()
                .RegisterType<IStartable, DefaultServiceBus>();
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