using System;
using System.Linq;
using System.Messaging;
using System.Transactions;
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
using StructureMap;
using ErrorAction = Rhino.ServiceBus.Msmq.TransportActions.ErrorAction;
using LoadBalancerConfiguration = Rhino.ServiceBus.LoadBalancer.LoadBalancerConfiguration;
using System.Collections.Generic;
using System.Reflection;

namespace Rhino.ServiceBus.StructureMap
{
    [CLSCompliant(false)]
    public class StructureMapBuilder : IBusContainerBuilder
    {
        private readonly AbstractRhinoServiceBusConfiguration config;
        private readonly IContainer container;

        public StructureMapBuilder(AbstractRhinoServiceBusConfiguration config, IContainer container)
        {
            this.config = config;
            this.container = container;
            config.BuildWith(this);
        }

        public void RegisterDefaultServices(IEnumerable<Assembly> assemblies)
        {
            container.Configure(c =>
            {
                c.For<IServiceLocator>().Use<StructureMapServiceLocator>();
                foreach (var assembly in assemblies)
                    c.Scan(s =>
                    {
                        s.Assembly(assembly);
                        s.AddAllTypesOf<IBusConfigurationAware>();
                    });
            });

            var locator = container.GetInstance<IServiceLocator>();
            foreach (var busConfigurationAware in container.GetAllInstances<IBusConfigurationAware>())
                busConfigurationAware.Configure(config, this, locator);

            container.Configure(c =>
            {
                foreach (var messageModule in config.MessageModules)
                {
                    Type module = messageModule;
                    if (!container.Model.HasImplementationsFor(module))
                    {
                        c.For(typeof(IMessageModule)).Singleton().Use(module).Named(typeof(IMessageModule).FullName);
                    }
                }

                c.For<IReflection>().Singleton().Use<DefaultReflection>();
                c.For(typeof(IMessageSerializer)).Singleton().Use(config.SerializerType);
                c.For<IEndpointRouter>().Singleton().Use<EndpointRouter>();
            });
        }

        public void WithInterceptor(IConsumerInterceptor interceptor)
        {
            container.Configure(c =>
            {
                c.RegisterInterceptor(new ConsumerInterceptor(interceptor, container));
            });
        }

        public void RegisterBus()
        {
            var busConfig = (RhinoServiceBusConfiguration)config;
            container.Configure(c =>
            {
                c.For<IDeploymentAction>().Use<CreateQueuesAction>();
                c.For<IStartableServiceBus>().Singleton().Use<DefaultServiceBus>()
                    .Ctor<MessageOwner[]>().Is(busConfig.MessageOwners.ToArray());
                c.Forward<IStartableServiceBus, IStartable>();
                c.Forward<IStartableServiceBus, IServiceBus>();
                c.For<IStartable>().Singleton();
                c.For<IServiceBus>().Singleton();
            });
        }

        public void RegisterPrimaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            container.Configure(c =>
            {
                c.For<MsmqLoadBalancer>().Singleton().Use<MsmqLoadBalancer>()
                    .Ctor<int>("threadCount").Is(loadBalancerConfig.ThreadCount)
                    .Ctor<TransactionalOptions>("transactional").Is(loadBalancerConfig.Transactional)
                    .Ctor<Uri>("secondaryLoadBalancer").Is(x => loadBalancerConfig.Endpoint)
                    .Ctor<Uri>("endpoint").Is(loadBalancerConfig.Endpoint);
                c.Forward<MsmqLoadBalancer, IStartable>();
                c.For<IDeploymentAction>().Use<CreateLoadBalancerQueuesAction>();
            });
        }

        public void RegisterSecondaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            container.Configure(c =>
            {
                c.For<MsmqLoadBalancer>().Singleton().Use<MsmqSecondaryLoadBalancer>()
                    .Ctor<int>("threadCount").Is(loadBalancerConfig.ThreadCount)
                    .Ctor<Uri>("primaryLoadBalancerUri").Is(x => loadBalancerConfig.PrimaryLoadBalancer)
                    .Ctor<TransactionalOptions>("transactional").Is(loadBalancerConfig.Transactional)
                    .Ctor<Uri>("endpoint").Is(loadBalancerConfig.Endpoint);
                c.Forward<MsmqLoadBalancer, IStartable>();
                c.For<IDeploymentAction>().Use<CreateLoadBalancerQueuesAction>();
            });
        }

        public void RegisterReadyForWork()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration)config;
            container.Configure(c =>
            {
                c.For<MsmqReadyForWorkListener>().Singleton().Use<MsmqReadyForWorkListener>()
                    .Ctor<Uri>().Is(loadBalancerConfig.ReadyForWork)
                    .Ctor<int>().Is(loadBalancerConfig.ThreadCount)
                    .Ctor<TransactionalOptions>().Is(loadBalancerConfig.Transactional);
                c.For<IDeploymentAction>().Use<CreateReadyForWorkQueuesAction>();
            });
        }

        public void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint)
        {
            container.Configure(c => c.For<LoadBalancerMessageModule>().Singleton().Use<LoadBalancerMessageModule>()
                                         .Ctor<Uri>().Is(loadBalancerEndpoint));
        }

        public void RegisterLoggingEndpoint(Uri logEndpoint)
        {
            container.Configure(c =>
            {
                c.ForSingletonOf<MessageLoggingModule>().Use(ctx => new MessageLoggingModule(ctx.GetInstance<IEndpointRouter>(), logEndpoint));
                c.Forward<MessageLoggingModule, IMessageModule>();
                c.For<IDeploymentAction>().Use<CreateLogQueueAction>();
            });
        }

        public void RegisterSingleton<T>(Func<T> func)
            where T : class
        {
            T singleton = null;
            container.Configure(c =>
            {
                c.For<T>().Use(x => singleton == null ? singleton = func() : singleton);
            });
        }
        public void RegisterSingleton<T>(string name, Func<T> func)
            where T : class
        {
            T singleton = null;
            container.Configure(c =>
            {
                c.For<T>().Use(x => singleton == null ? singleton = func() : singleton).Named(name);
            });
        }

        public void RegisterAll<T>(params Type[] excludes)
            where T : class { RegisterAll<T>((Predicate<Type>)(x => !x.IsAbstract && !x.IsInterface && !excludes.Contains(x))); }
        public void RegisterAll<T>(Predicate<Type> condition)
            where T : class
        {
            container.Configure(c =>
            {
                c.Scan(s =>
                {
                    s.Assembly(typeof(T).Assembly);
                    s.With(new SingletonConvention<T>());
                    s.AddAllTypesOf<T>();
                    s.Exclude(t => !condition(t));
                });
            });
        }

        public void RegisterSecurity(byte[] key)
        {
            container.Configure(c =>
            {
                c.For<IEncryptionService>().Singleton().Use<RijndaelEncryptionService>()
                    .Ctor<byte[]>().Is(key);
                c.For<IValueConvertor<WireEncryptedString>>().Singleton().Use<WireEncryptedStringConvertor>();
                c.For<IElementSerializationBehavior>().Singleton().Use<WireEncryptedMessageConvertor>();
            });
        }

        public void RegisterNoSecurity()
        {
            container.Configure(c =>
            {
                c.For<IValueConvertor<WireEncryptedString>>().Singleton().Use<ThrowingWireEncryptedStringConvertor>();
                c.For<IElementSerializationBehavior>().Singleton().Use<ThrowingWireEncryptedMessageConvertor>();
            });
        }
    }
}