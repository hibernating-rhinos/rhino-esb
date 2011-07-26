using System;
using System.Linq;
using System.Messaging;
using System.Transactions;
using Microsoft.Practices.Unity;
using Rhino.Queues;
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
using Rhino.ServiceBus.RhinoQueues;
using ErrorAction = Rhino.ServiceBus.Msmq.TransportActions.ErrorAction;
using IStartable = Rhino.ServiceBus.Internal.IStartable;
using LoadBalancerConfiguration = Rhino.ServiceBus.LoadBalancer.LoadBalancerConfiguration;

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

        public void RegisterDefaultServices()
        {
            if (!container.IsRegistered(typeof(IUnityContainer)))
                container.RegisterInstance(container);

            container.RegisterType<IServiceLocator, UnityServiceLocator>();
            container.RegisterTypesFromAssembly<IBusConfigurationAware>(typeof(IServiceBus).Assembly);

            foreach (var configurationAware in container.ResolveAll<IBusConfigurationAware>())
            {
                configurationAware.Configure(config, this);
            }

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
                    new ResolvedParameter<MessageLoggingModule>(typeof(MessageLoggingModule).FullName),
                    new ResolvedParameter<ITransport>()));
        }

        public void RegisterMsmqTransport(Type queueStrategyType)
        {
            if (queueStrategyType.Equals(typeof(FlatQueueStrategy)))
            {
                container.RegisterType(typeof(IQueueStrategy), queueStrategyType,
                                       new ContainerControlledLifetimeManager(),
                                       new InjectionConstructor(
                                           new ResolvedParameter<IEndpointRouter>(),
                                           new InjectionParameter<Uri>(config.Endpoint)));
            }
            else
            {
                container.RegisterType(typeof(IQueueStrategy), queueStrategyType);
            }

            container.RegisterType<IMessageBuilder<Message>, MsmqMessageBuilder>(
                new ContainerControlledLifetimeManager());
            container.RegisterType<IMsmqTransportAction, ErrorAction>(Guid.NewGuid().ToString(),
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new InjectionParameter<int>(config.NumberOfRetries),
                    new ResolvedParameter<IQueueStrategy>()));
            container.RegisterType<ISubscriptionStorage, MsmqSubscriptionStorage>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IReflection>(),
                    new ResolvedParameter<IMessageSerializer>(),
                    new InjectionParameter<Uri>(config.Endpoint),
                    new ResolvedParameter<IEndpointRouter>(),
                    new ResolvedParameter<IQueueStrategy>()));
            container.RegisterType<ITransport, MsmqTransport>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IMessageSerializer>(),
                    new ResolvedParameter<IQueueStrategy>(),
                    new InjectionParameter<Uri>(config.Endpoint),
                    new InjectionParameter<int>(config.ThreadCount),
                    new ResolvedParameter<IMsmqTransportAction[]>(),
                    new ResolvedParameter<IEndpointRouter>(),
                    new InjectionParameter<IsolationLevel>(config.IsolationLevel),
                    new InjectionParameter<TransactionalOptions>(config.Transactional),
                    new InjectionParameter<bool>(config.ConsumeInTransaction),
                    new ResolvedParameter<IMessageBuilder<Message>>()));

            container.RegisterTypesFromAssembly<IMsmqTransportAction>(typeof(IMsmqTransportAction).Assembly, typeof(ErrorAction));
        }

        public void RegisterQueueCreation()
        {
            container.RegisterType<IServiceBusAware, QueueCreationModule>(typeof(QueueCreationModule).FullName, new ContainerControlledLifetimeManager());
        }

        public void RegisterMsmqOneWay()
        {
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration)config;
            container.RegisterType<IMessageBuilder<Message>, MsmqMessageBuilder>(
                new ContainerControlledLifetimeManager());
            container.RegisterType<IOnewayBus, MsmqOnewayBus>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new InjectionParameter<MessageOwner[]>(oneWayConfig.MessageOwners),
                    new ResolvedParameter<IMessageBuilder<Message>>()
                    ));
        }

        public void RegisterRhinoQueuesTransport()
        {
            var busConfig = config.ConfigurationSection.Bus;
            container.RegisterType<ISubscriptionStorage, PhtSubscriptionStorage>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new InjectionParameter<string>(busConfig.SubscriptionPath),
                    new ResolvedParameter<IMessageSerializer>(),
                    new ResolvedParameter<IReflection>()));

            container.RegisterType<ITransport, RhinoQueuesTransport>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new InjectionParameter<Uri>(config.Endpoint),
                    new ResolvedParameter<IEndpointRouter>(),
                    new ResolvedParameter<IMessageSerializer>(),
                    new InjectionParameter<int>(config.ThreadCount),
                    new InjectionParameter<string>(busConfig.QueuePath),
                    new InjectionParameter<IsolationLevel>(config.IsolationLevel),
                    new InjectionParameter<int>(config.NumberOfRetries),
                    new InjectionParameter<bool>(busConfig.EnablePerformanceCounters),
                    new ResolvedParameter<IMessageBuilder<MessagePayload>>()));

            container.RegisterType<IMessageBuilder<MessagePayload>, RhinoQueuesMessageBuilder>(
                new ContainerControlledLifetimeManager());
        }

        public void RegisterRhinoQueuesOneWay()
        {
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration)config;
            var busConfig = config.ConfigurationSection.Bus;

            container.RegisterType<IMessageBuilder<MessagePayload>, RhinoQueuesMessageBuilder>(
                new ContainerControlledLifetimeManager());
            container.RegisterType<IOnewayBus, RhinoQueuesOneWayBus>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new InjectionParameter<MessageOwner[]>(oneWayConfig.MessageOwners),
                    new ResolvedParameter<IMessageSerializer>(),
                    new InjectionParameter<string>(busConfig.QueuePath),
                    new InjectionParameter<bool>(busConfig.EnablePerformanceCounters),
                    new ResolvedParameter<IMessageBuilder<MessagePayload>>()));
        }

        public void RegisterSecurity(byte[] key)
        {
            container.RegisterType<IEncryptionService, RijndaelEncryptionService>("esb.security",
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new InjectionParameter<byte[]>(key)));

            container.RegisterType<IValueConvertor<WireEcryptedString>, WireEcryptedStringConvertor>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IEncryptionService>("esb.security")));
            container.RegisterType<IElementSerializationBehavior, WireEncryptedMessageConvertor>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IEncryptionService>("esb.security")));
        }

        public void RegisterNoSecurity()
        {
            container.RegisterType<IValueConvertor<WireEcryptedString>, ThrowingWireEcryptedStringConvertor>(
                new ContainerControlledLifetimeManager());
            container.RegisterType<IElementSerializationBehavior, ThrowingWireEncryptedMessageConvertor>(
                new ContainerControlledLifetimeManager());
        }
    }
}