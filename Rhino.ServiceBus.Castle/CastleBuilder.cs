using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
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

        public void RegisterDefaultServices()
        {
            if (!container.Kernel.HasComponent(typeof(IWindsorContainer)))
                container.Register(Component.For<IWindsorContainer>().Instance(container));

            container.Register(Component.For<IServiceLocator>().ImplementedBy<CastleServiceLocator>());

            container.Register(
                AllTypes.FromAssembly(typeof(IServiceBus).Assembly)
                    .BasedOn<IBusConfigurationAware>()
                );

            foreach (var configurationAware in container.ResolveAll<IBusConfigurationAware>())
            {
                configurationAware.Configure(config, this);
            }

            container.Kernel.Resolver.AddSubResolver(new ArrayResolver(container.Kernel));

            foreach (var type in config.MessageModules)
            {
                if (container.Kernel.HasComponent(type) == false)
                    container.Register(Component.For(type).Named(type.FullName));
            }

            container.Register(
                Component.For<IReflection>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy<DefaultReflection>(),

                Component.For<IMessageSerializer>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(config.SerializerType),
                Component.For<IEndpointRouter>()
                    .ImplementedBy<EndpointRouter>()
                );
        }

        public void RegisterBus()
        {
            var busConfig = (RhinoServiceBusConfiguration) config;

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
                    .Parameters(
                        Parameter.ForKey("modules").Eq(CreateModuleConfigurationNode(busConfig.MessageModules))
                    )
                );
        }

        private static IConfiguration CreateModuleConfigurationNode(IEnumerable<Type> messageModules)
        {
            var config = new MutableConfiguration("array");
            foreach (Type type in messageModules)
            {
                config.CreateChild("item", "${" + type.FullName + "}");
            }
            return config;
        }

        public void RegisterPrimaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration) config;
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
                    .ImplementedBy<CreateLoadBalancerQueuesAction>()
                );
        }

        public void RegisterSecondaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration) config;
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
                    .ImplementedBy<CreateLoadBalancerQueuesAction>()
                );
        }

        public void RegisterReadyForWork()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration) config;
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
                    .ImplementedBy<CreateReadyForWorkQueuesAction>()
                );
        }

        public void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint)
        {
            container.Register(
                Component.For<LoadBalancerMessageModule>()
                    .DependsOn(new {loadBalancerEndpoint})
                );
        }

        public void RegisterLoggingEndpoint(Uri logEndpoint)
        {
            container.Register(
                Component.For<MessageLoggingModule>()
                    .DependsOn(new {logQueue = logEndpoint})
                );
        }

        public void RegisterMsmqTransport(Type queueStrategyType)
        {
            container.Kernel.Register(
                Component.For<IQueueStrategy>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(queueStrategyType)
                    .DependsOn(new {endpoint = config.Endpoint}),
                Component.For<IMessageBuilder<Message>>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy<MsmqMessageBuilder>(),
                Component.For<IMsmqTransportAction>()
                    .ImplementedBy<ErrorAction>()
                    .DependsOn(new { numberOfRetries = config.NumberOfRetries}),
                Component.For<ISubscriptionStorage>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(MsmqSubscriptionStorage))
                    .DependsOn(new
                    {
                        queueBusListensTo = config.Endpoint
                    }),
                Component.For<ITransport>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(MsmqTransport))
                    .DependsOn(new
                    {
                        threadCount = config.ThreadCount,
                        endpoint = config.Endpoint,
                        queueIsolationLevel = config.IsolationLevel,
                        transactional = config.Transactional,
                        consumeInTransaction = config.ConsumeInTransaction,
                    }),
                AllTypes.FromAssembly(typeof(IMsmqTransportAction).Assembly)
                    .BasedOn<IMsmqTransportAction>()
                    .Unless(x => x == typeof(ErrorAction))
                    .WithService.FirstInterface()
                    .Configure(registration =>
                               registration.LifeStyle.Is(LifestyleType.Singleton))
                );
        }

        public void RegisterQueueCreation()
        {
            container.Kernel.Register(Component.For<QueueCreationModule>());
        }

        public void RegisterMsmqOneWay()
        {
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration) config;
            container.Register(
                   Component.For<IMessageBuilder<Message>>()
                       .LifeStyle.Is(LifestyleType.Singleton)
                       .ImplementedBy<MsmqMessageBuilder>(),
                   Component.For<IOnewayBus>()
                       .LifeStyle.Is(LifestyleType.Singleton)
                       .ImplementedBy<MsmqOnewayBus>()
                       .DependsOn(new { messageOwners = oneWayConfig.MessageOwners }));
        }

        public void RegisterRhinoQueuesTransport()
        {
            var busConfig = config.ConfigurationSection.Bus;
            container.Register(
                Component.For<ISubscriptionStorage>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(PhtSubscriptionStorage))
                    .DependsOn(new
                    {
                        subscriptionPath = busConfig.SubscriptionPath
                    }),
                Component.For<ITransport>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(RhinoQueuesTransport))
                    .DependsOn(new
                    {
                        threadCount = config.ThreadCount,
                        endpoint = config.Endpoint,
                        queueIsolationLevel = config.IsolationLevel,
                        numberOfRetries = config.NumberOfRetries,
                        path = busConfig.QueuePath,
                        enablePerformanceCounters = busConfig.EnablePerformanceCounters
                    }),
                Component.For<IMessageBuilder<MessagePayload>>()
                    .ImplementedBy<RhinoQueuesMessageBuilder>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                );
        }

        public void RegisterRhinoQueuesOneWay()
        {
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration) config;
            var busConfig = config.ConfigurationSection.Bus;
            container.Register(
                     Component.For<IMessageBuilder<MessagePayload>>()
                        .ImplementedBy<RhinoQueuesMessageBuilder>()
                        .LifeStyle.Is(LifestyleType.Singleton),
                    Component.For<IOnewayBus>()
                        .LifeStyle.Is(LifestyleType.Singleton)
                        .ImplementedBy<RhinoQueuesOneWayBus>()
                        .DependsOn(new
                        {
                            messageOwners = oneWayConfig.MessageOwners.ToArray(),
                            path = busConfig.QueuePath,
                            enablePerformanceCounters = busConfig.EnablePerformanceCounters
                        })
                    );
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
					.Named("esb.security")
				);

            container.Register(
                Component.For<IValueConvertor<WireEcryptedString>>()
                    .ImplementedBy<WireEcryptedStringConvertor>()
					.ServiceOverrides(ServiceOverride.ForKey("encryptionService").Eq("esb.security"))
                );

        	container.Register(
				Component.For<IElementSerializationBehavior>()
					.ImplementedBy<WireEncryptedMessageConvertor>()
					.ServiceOverrides(ServiceOverride.ForKey("encryptionService").Eq("esb.security"))
        		);
        }

        public void RegisterNoSecurity()
        {
            container.Register(
                   Component.For<IValueConvertor<WireEcryptedString>>()
                       .ImplementedBy<ThrowingWireEcryptedStringConvertor>()
                   );
            container.Register(
                Component.For<IElementSerializationBehavior>()
                    .ImplementedBy<ThrowingWireEncryptedMessageConvertor>()
                );
        }
    }
}