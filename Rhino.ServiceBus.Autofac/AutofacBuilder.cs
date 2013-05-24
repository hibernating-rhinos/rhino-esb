using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using Autofac;
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
using LoadBalancerConfiguration = Rhino.ServiceBus.LoadBalancer.LoadBalancerConfiguration;
using System.Reflection;

namespace Rhino.ServiceBus.Autofac
{
    public class AutofacBuilder : IBusContainerBuilder
    {
        private readonly AbstractRhinoServiceBusConfiguration config;
        private readonly IContainer container;

        public AutofacBuilder(AbstractRhinoServiceBusConfiguration config, IContainer container)
        {
            this.config = config;
            this.container = container;
            config.BuildWith(this);
        }

        public void WithInterceptor(IConsumerInterceptor interceptor)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new ConsumerInterceptorModule(interceptor));
            builder.Update(container);
        }

        public void RegisterDefaultServices(IEnumerable<Assembly> assemblies)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(container);
            builder.RegisterType<AutofacServiceLocator>()
                .As<IServiceLocator>()
                .SingleInstance();
            foreach (var assembly in assemblies)
                builder.RegisterAssemblyTypes(assembly)
                    .AssignableTo<IBusConfigurationAware>()
                    .As<IBusConfigurationAware>()
                    .SingleInstance();
            builder.RegisterType<DefaultReflection>()
                .As<IReflection>()
                .SingleInstance();
            builder.RegisterType(config.SerializerType)
                .As<IMessageSerializer>()
                .SingleInstance();
            builder.RegisterType<EndpointRouter>()
                .As<IEndpointRouter>()
                .SingleInstance();
            foreach (var module in config.MessageModules)
            {
                builder.RegisterType(module)
                    .Named<IMessageModule>(module.FullName)
                    .As<IMessageModule>()
                    .SingleInstance();
            }
            builder.Update(container);

            var locator = container.Resolve<IServiceLocator>();
            foreach (var busConfigurationAware in container.Resolve<IEnumerable<IBusConfigurationAware>>())
                busConfigurationAware.Configure(config, this, locator);
        }

        public void RegisterBus()
        {
            var builder = new ContainerBuilder();
            var busConfig = (RhinoServiceBusConfiguration)config;
            builder.RegisterType<DefaultServiceBus>()
                .WithParameter("messageOwners", busConfig.MessageOwners.ToArray())
                .AsImplementedInterfaces()
                .SingleInstance();
            builder.RegisterType<CreateLogQueueAction>()
                .As<IDeploymentAction>()
                .SingleInstance();
            builder.RegisterType<CreateQueuesAction>()
                .As<IDeploymentAction>()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterPrimaryLoadBalancer()
        {
            var builder = new ContainerBuilder();
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            builder.RegisterType<MsmqLoadBalancer>()
                .WithParameter("endpoint", loadBalancerConfig.Endpoint)
                .WithParameter("threadCount", loadBalancerConfig.ThreadCount)
                .WithParameter("secondaryLoadBalancer", loadBalancerConfig.Endpoint)
                .WithParameter("transactional", loadBalancerConfig.Transactional)
                .PropertiesAutowired()
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance();
            builder.RegisterType<CreateLoadBalancerQueuesAction>()
                .As<IDeploymentAction>()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterSecondaryLoadBalancer()
        {
            var builder = new ContainerBuilder();
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            builder.RegisterType<MsmqSecondaryLoadBalancer>()
                .WithParameter("endpoint", loadBalancerConfig.Endpoint)
                .WithParameter("primaryLoadBalancer", loadBalancerConfig.PrimaryLoadBalancer)
                .WithParameter("threadCount", loadBalancerConfig.ThreadCount)
                .WithParameter("transactional", loadBalancerConfig.Transactional)
                .PropertiesAutowired()
                .As<MsmqLoadBalancer>()
                .AsImplementedInterfaces()
                .SingleInstance();
            builder.RegisterType<CreateLoadBalancerQueuesAction>()
                .As<IDeploymentAction>()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterReadyForWork()
        {
            var builder = new ContainerBuilder();
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            builder.RegisterType<MsmqReadyForWorkListener>()
                .WithParameter("endpoint", loadBalancerConfig.ReadyForWork)
                .WithParameter("threadCount", loadBalancerConfig.ThreadCount)
                .WithParameter("transactional", loadBalancerConfig.Transactional)
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<CreateReadyForWorkQueuesAction>()
                .As<IDeploymentAction>()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint)
        {
            var builder = new ContainerBuilder();
            builder.Register(c => new LoadBalancerMessageModule(loadBalancerEndpoint, c.Resolve<IEndpointRouter>()))
                .As<IMessageModule>()
                .AsSelf()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterLoggingEndpoint(Uri logEndpoint)
        {
            var builder = new ContainerBuilder();
            builder.Register(c => new MessageLoggingModule(c.Resolve<IEndpointRouter>(), logEndpoint))
                .As<IMessageModule>()
                .AsSelf()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterSingleton<T>(Func<T> func)
            where T : class
        {
            T singleton = null;
            var builder = new ContainerBuilder();
            builder.Register(x => singleton == null ? singleton = func() : singleton)
                .As<T>()
                .SingleInstance();
            builder.Update(container);
        }
        public void RegisterSingleton<T>(string name, Func<T> func)
            where T : class
        {
            T singleton = null;
            var builder = new ContainerBuilder();
            builder.Register(x => singleton == null ? singleton = func() : singleton)
                .As<T>()
                .Named<T>(name)
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterAll<T>(params Type[] excludes)
            where T : class { RegisterAll<T>((Predicate<Type>)(x => !x.IsAbstract && !x.IsInterface && typeof(T).IsAssignableFrom(x) && !excludes.Contains(x))); }
        public void RegisterAll<T>(Predicate<Type> condition)
            where T : class
        {
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(typeof(T).Assembly)
                .Where(x => condition(x))
                .As<T>()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterSecurity(byte[] key)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<RijndaelEncryptionService>()
                .WithParameter(new NamedParameter("key", key))
                .As<IEncryptionService>().SingleInstance();
            builder.RegisterType<WireEncryptedStringConvertor>()
                .As<IValueConvertor<WireEncryptedString>>()
                .SingleInstance();
            builder.RegisterType<WireEncryptedMessageConvertor>()
                .As<IElementSerializationBehavior>().SingleInstance();
            builder.Update(container);
        }

        public void RegisterNoSecurity()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ThrowingWireEncryptedStringConvertor>()
                .As<IValueConvertor<WireEncryptedString>>().SingleInstance();
            builder.RegisterType<ThrowingWireEncryptedMessageConvertor>()
                .As<IElementSerializationBehavior>().SingleInstance();
            builder.Update(container);
        }
    }
}