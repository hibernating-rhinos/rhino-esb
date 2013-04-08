using System;
using System.Linq;
using System.Messaging;
using System.Transactions;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Convertors;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Msmq.TransportActions;
using ErrorAction = Rhino.ServiceBus.Msmq.TransportActions.ErrorAction;
using IStartable = Rhino.ServiceBus.Internal.IStartable;
using LoadBalancerConfiguration = Rhino.ServiceBus.LoadBalancer.LoadBalancerConfiguration;
using System.Reflection;
using System.Collections.Generic;

namespace Rhino.ServiceBus.Unity
{
    public class UnityBuilder : IBusContainerBuilder
    {
        private readonly IUnityContainer container;
        private readonly AbstractRhinoServiceBusConfiguration config;

        public UnityBuilder(IUnityContainer container, AbstractRhinoServiceBusConfiguration config)
        {
            this.container = container;
            this.config = config;
            this.config.BuildWith(this);
        }

        public void WithInterceptor(IConsumerInterceptor interceptor)
        {
            container.AddExtension(new ConsumerExtension(interceptor));
        }

        public void RegisterDefaultServices(IEnumerable<Assembly> assemblies)
        {
            if (!container.IsRegistered(typeof(IUnityContainer)))
                container.RegisterInstance(container);

            container.RegisterType<IServiceLocator, UnityServiceLocator>();
            foreach (var assembly in assemblies)
                container.RegisterTypesFromAssembly<IBusConfigurationAware>(assembly);

            var locator = container.Resolve<IServiceLocator>();
            foreach (var configurationAware in container.ResolveAll<IBusConfigurationAware>())
                configurationAware.Configure(config, this, locator);

            foreach (var messageModule in config.MessageModules)
            {
                Type module = messageModule;
                if (!container.IsRegistered(module))
                    container.RegisterType(typeof(IMessageModule), module, module.FullName, new ContainerControlledLifetimeManager());
            }

            container.RegisterType<IReflection, DefaultReflection>(new ContainerControlledLifetimeManager());
            container.RegisterType(typeof(IMessageSerializer), config.SerializerType, new ContainerControlledLifetimeManager());
            container.RegisterType<IEndpointRouter, EndpointRouter>(new ContainerControlledLifetimeManager());
        }

        public void RegisterBus()
        {
            var busConfig = (RhinoServiceBusConfiguration)config;

            container.RegisterType<IDeploymentAction, CreateQueuesAction>(Guid.NewGuid().ToString(), new ContainerControlledLifetimeManager());

            container.RegisterType<DefaultServiceBus>(new ContainerControlledLifetimeManager())
                .RegisterType<IStartableServiceBus, DefaultServiceBus>(
                    new InjectionConstructor(
                        new ResolvedParameter<IServiceLocator>(),
                        new ResolvedParameter<ITransport>(),
                        new ResolvedParameter<ISubscriptionStorage>(),
                        new ResolvedParameter<IReflection>(),
                        new ResolvedParameter<IMessageModule[]>(),
                        new InjectionParameter<MessageOwner[]>(busConfig.MessageOwners.ToArray()),
                        new ResolvedParameter<IEndpointRouter>()))
                .RegisterType<IServiceBus, DefaultServiceBus>()
                .RegisterType<IStartable, DefaultServiceBus>();
        }

        public void RegisterPrimaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            container.RegisterType<MsmqLoadBalancer>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IMessageSerializer>(),
                    new ResolvedParameter<IQueueStrategy>(),
                    new ResolvedParameter<IEndpointRouter>(),
                    new InjectionParameter<Uri>(loadBalancerConfig.Endpoint),
                    new InjectionParameter<int>(loadBalancerConfig.ThreadCount),
                    new InjectionParameter<Uri>(loadBalancerConfig.SecondaryLoadBalancer),
                    new InjectionParameter<TransactionalOptions>(loadBalancerConfig.Transactional),
                    new ResolvedParameter<IMessageBuilder<Message>>()),
                new InjectionProperty("ReadyForWorkListener"))
                .RegisterType<IStartable, MsmqLoadBalancer>(new ContainerControlledLifetimeManager());

            container.RegisterType<IDeploymentAction, CreateLoadBalancerQueuesAction>(Guid.NewGuid().ToString(), new ContainerControlledLifetimeManager());
        }

        public void RegisterSecondaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            container.RegisterType<MsmqSecondaryLoadBalancer>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IMessageSerializer>(),
                    new ResolvedParameter<IQueueStrategy>(),
                    new ResolvedParameter<IEndpointRouter>(),
                    new InjectionParameter<Uri>(loadBalancerConfig.Endpoint),
                    new InjectionParameter<Uri>(loadBalancerConfig.PrimaryLoadBalancer),
                    new InjectionParameter<int>(loadBalancerConfig.ThreadCount),
                    new InjectionParameter<TransactionalOptions>(loadBalancerConfig.Transactional),
                    new ResolvedParameter<IMessageBuilder<Message>>()))
                .RegisterType<IStartable, MsmqSecondaryLoadBalancer>(new ContainerControlledLifetimeManager());

            container.RegisterType<IDeploymentAction, CreateLoadBalancerQueuesAction>(Guid.NewGuid().ToString(), new ContainerControlledLifetimeManager());
        }

        public void RegisterReadyForWork()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            container.RegisterType<MsmqReadyForWorkListener>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IQueueStrategy>(),
                    new InjectionParameter<Uri>(loadBalancerConfig.ReadyForWork),
                    new InjectionParameter<int>(loadBalancerConfig.ThreadCount),
                    new ResolvedParameter<IMessageSerializer>(),
                    new ResolvedParameter<IEndpointRouter>(),
                    new InjectionParameter<TransactionalOptions>(loadBalancerConfig.Transactional),
                    new ResolvedParameter<IMessageBuilder<Message>>()));

            container.RegisterType<IDeploymentAction, CreateReadyForWorkQueuesAction>(Guid.NewGuid().ToString(), new ContainerControlledLifetimeManager());
        }

        public void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint)
        {
            container.RegisterType<LoadBalancerMessageModule>(typeof(LoadBalancerMessageModule).FullName,
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new InjectionParameter<Uri>(loadBalancerEndpoint),
                    new ResolvedParameter<IEndpointRouter>()));
        }

        public void RegisterLoggingEndpoint(Uri logEndpoint)
        {
            container.RegisterType<MessageLoggingModule>(typeof(MessageLoggingModule).FullName,
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IEndpointRouter>(),
                    new InjectionParameter<Uri>(logEndpoint)));

            container.RegisterType<IDeploymentAction, CreateLogQueueAction>(Guid.NewGuid().ToString(),
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IQueueStrategy>(),
                    new ResolvedParameter<MessageLoggingModule>(typeof(MessageLoggingModule).FullName),
                    new ResolvedParameter<ITransport>()));
        }

        public void RegisterSingleton<T>(Func<T> func)
            where T : class
        {
            T singleton = null;
            container.RegisterType<T>(new InjectionFactory(x => singleton == null ? singleton = func() : singleton));
        }
        public void RegisterSingleton<T>(string name, Func<T> func)
            where T : class
        {
            T singleton = null;
            container.RegisterType<T>(name, new InjectionFactory(x => singleton == null ? singleton = func() : singleton));
        }

        public void RegisterAll<T>(params Type[] excludes)
            where T : class { RegisterAll<T>((Predicate<Type>)(x => !x.IsAbstract && !x.IsInterface && typeof(T).IsAssignableFrom(x) && !excludes.Contains(x))); }
        public void RegisterAll<T>(Predicate<Type> condition)
            where T : class
        {
            container.RegisterTypesFromAssembly<T>(typeof(T).Assembly, condition);
        }

        public void RegisterSecurity(byte[] key)
        {
            container.RegisterType<IEncryptionService, RijndaelEncryptionService>("esb.security",
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new InjectionParameter<byte[]>(key)));

            container.RegisterType<IValueConvertor<WireEncryptedString>, WireEncryptedStringConvertor>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IEncryptionService>("esb.security")));
            container.RegisterType<IElementSerializationBehavior, WireEncryptedMessageConvertor>("WireEncryptedMessageConvertor",
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IEncryptionService>("esb.security")));
        }

        public void RegisterNoSecurity()
        {
            container.RegisterType<IValueConvertor<WireEncryptedString>, ThrowingWireEncryptedStringConvertor>(
                new ContainerControlledLifetimeManager());
            container.RegisterType<IElementSerializationBehavior, ThrowingWireEncryptedMessageConvertor>("ThrowingWireEncryptedMessageConvertor",
                new ContainerControlledLifetimeManager());
        }
    }
}
