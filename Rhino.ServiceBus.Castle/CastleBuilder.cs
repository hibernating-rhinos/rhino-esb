using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
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

namespace Rhino.ServiceBus.Castle
{
    public class CastleBuilder : IBusContainerBuilder
    {
        private readonly IWindsorContainer container;
        private readonly AbstractRhinoServiceBusConfiguration config;

        public CastleBuilder(IWindsorContainer container, AbstractRhinoServiceBusConfiguration config)
        {
            this.container = container;
            this.config = config;
            this.config.BuildWith(this);
        }

        public void WithInterceptor(IConsumerInterceptor interceptor)
        {
            container.Kernel.ComponentModelCreated +=
                model =>
                {
                    if (typeof(IMessageConsumer).IsAssignableFrom(model.Implementation) == false)
                        return;

                    model.LifestyleType = LifestyleType.Transient;
                    interceptor.ItemCreated(model.Implementation, true);
                };
        }

        public void RegisterDefaultServices(IEnumerable<Assembly> assemblies)
        {
            if (!container.Kernel.HasComponent(typeof(IWindsorContainer)))
                container.Register(Component.For<IWindsorContainer>().Instance(container));

            container.Register(Component.For<IServiceLocator>().ImplementedBy<CastleServiceLocator>());
            foreach (var assembly in assemblies)
                container.Register(
                    AllTypes.FromAssembly(assembly)
                        .BasedOn<IBusConfigurationAware>().WithService.FirstInterface()
                    );

            var locator = container.Resolve<IServiceLocator>();
            foreach (var configurationAware in container.ResolveAll<IBusConfigurationAware>())
                configurationAware.Configure(config, this, locator);

            container.Kernel.Resolver.AddSubResolver(new ArrayResolver(container.Kernel));

            foreach (var type in config.MessageModules)
                if (!container.Kernel.HasComponent(type))
                    container.Register(Component.For(type).Named(type.FullName));

            container.Register(
                Component.For<IReflection>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy<CastleReflection>(),

                Component.For<IMessageSerializer>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(config.SerializerType),
                Component.For<IEndpointRouter>()
                    .ImplementedBy<EndpointRouter>());
        }

        public void RegisterBus()
        {
            var busConfig = (RhinoServiceBusConfiguration)config;

            container.Register(
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateLogQueueAction>(),
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateQueuesAction>(),
                Component.For<IServiceBus, IStartableServiceBus, IStartable>()
                    .ImplementedBy<DefaultServiceBus>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .DependsOn(new
                    {
                        messageOwners = busConfig.MessageOwners.ToArray(),
                    })
                    .DependsOn(Dependency.OnConfigValue("modules", CreateModuleConfigurationNode(busConfig.MessageModules))));
        }

        private static IConfiguration CreateModuleConfigurationNode(IEnumerable<Type> messageModules)
        {
            var config = new MutableConfiguration("array");
            foreach (Type type in messageModules)
                config.CreateChild("item", "${" + type.FullName + "}");
            return config;
        }

        public void RegisterPrimaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            container.Register(Component.For<MsmqLoadBalancer, IStartable>()
                                   .ImplementedBy(loadBalancerConfig.LoadBalancerType)
                                   .LifeStyle.Is(LifestyleType.Singleton)
                                   .DependsOn(new
                                   {
                                       endpoint = loadBalancerConfig.Endpoint,
                                       threadCount = loadBalancerConfig.ThreadCount,
                                       primaryLoadBalancer = loadBalancerConfig.PrimaryLoadBalancer,
                                       transactional = loadBalancerConfig.Transactional
                                   }));

            container.Register(
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateLoadBalancerQueuesAction>());
        }

        public void RegisterSecondaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            container.Register(Component.For<MsmqLoadBalancer>()
                                       .ImplementedBy(loadBalancerConfig.LoadBalancerType)
                                       .LifeStyle.Is(LifestyleType.Singleton)
                                       .DependsOn(new
                                       {
                                           endpoint = loadBalancerConfig.Endpoint,
                                           threadCount = loadBalancerConfig.ThreadCount,
                                           primaryLoadBalancer = loadBalancerConfig.PrimaryLoadBalancer,
                                           transactional = loadBalancerConfig.Transactional,
                                           secondaryLoadBalancer = loadBalancerConfig.SecondaryLoadBalancer,
                                       }));

            container.Register(
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateLoadBalancerQueuesAction>());
        }

        public void RegisterReadyForWork()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            container.Register(Component.For<MsmqReadyForWorkListener>()
                                    .LifeStyle.Is(LifestyleType.Singleton)
                                    .DependsOn(new
                                    {
                                        endpoint = loadBalancerConfig.ReadyForWork,
                                        threadCount = loadBalancerConfig.ThreadCount,
                                        transactional = loadBalancerConfig.Transactional
                                    }));
            container.Register(
            Component.For<IDeploymentAction>()
                .ImplementedBy<CreateReadyForWorkQueuesAction>());
        }

        public void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint)
        {
            container.Register(
                Component.For<LoadBalancerMessageModule>()
                    .DependsOn(new { loadBalancerEndpoint }));
        }

        public void RegisterLoggingEndpoint(Uri logEndpoint)
        {
            container.Register(
                Component.For<MessageLoggingModule>()
                    .DependsOn(new { logQueue = logEndpoint }));
        }

        public void RegisterSingleton<T>(Func<T> func)
            where T : class
        {
            T singleton = null;
            container.Register(
                Component.For<T>()
                    .UsingFactoryMethod<T>(x => singleton == null ? singleton = func() : singleton));
        }
        public void RegisterSingleton<T>(string name, Func<T> func)
            where T : class
        {
            T singleton = null;
            container.Register(
                Component.For<T>()
                    .UsingFactoryMethod<T>(x => singleton == null ? singleton = func() : singleton).Named(name));
        }

        public void RegisterAll<T>(params Type[] excludes)
            where T : class { RegisterAll<T>((Predicate<Type>)(x => !excludes.Contains(x))); }
        public void RegisterAll<T>(Predicate<Type> condition)
            where T : class
        {
            container.Kernel.Register(
                AllTypes.FromAssembly(typeof(T).Assembly)
                    .BasedOn<T>()
                    .Unless(x => !condition(x))
                    .WithService.FirstInterface()
                    .Configure(registration =>
                            registration.LifeStyle.Is(LifestyleType.Singleton)));
        }

        public void RegisterSecurity(byte[] key)
        {
            container.Register(
                Component.For<IEncryptionService>()
                    .ImplementedBy<RijndaelEncryptionService>()
                    .DependsOn(new
                    {
                        key,
                    })
                    .Named("esb.security"));

            container.Register(
                Component.For<IValueConvertor<WireEncryptedString>>()
                    .ImplementedBy<WireEncryptedStringConvertor>()
                    .DependsOn(Dependency.OnComponent("encryptionService", "esb.security")));

            container.Register(
                Component.For<IElementSerializationBehavior>()
                    .ImplementedBy<WireEncryptedMessageConvertor>()
                    .DependsOn(Dependency.OnComponent("encryptionService", "esb.security")));
        }

        public void RegisterNoSecurity()
        {
            container.Register(
                   Component.For<IValueConvertor<WireEncryptedString>>()
                       .ImplementedBy<ThrowingWireEncryptedStringConvertor>());
            container.Register(
                Component.For<IElementSerializationBehavior>()
                    .ImplementedBy<ThrowingWireEncryptedMessageConvertor>());
        }
    }
}