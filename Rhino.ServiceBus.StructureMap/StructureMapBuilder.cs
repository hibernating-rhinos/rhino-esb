using System;
using System.Linq;
using System.Messaging;
using System.Transactions;
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
using StructureMap;
using ErrorAction = Rhino.ServiceBus.Msmq.TransportActions.ErrorAction;
using LoadBalancerConfiguration = Rhino.ServiceBus.LoadBalancer.LoadBalancerConfiguration;

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

        public void RegisterDefaultServices()
        {
            container.Configure(c =>
            {
                c.For<IServiceLocator>().Use<StructureMapServiceLocator>();
                c.Scan(s =>
                {
                    s.AssemblyContainingType(typeof(IServiceBus));
                    s.AddAllTypesOf<IBusConfigurationAware>();
                });
            });

            foreach (var busConfigurationAware in container.GetAllInstances<IBusConfigurationAware>())
            {
                busConfigurationAware.Configure(config, this);
            }

            container.Configure(c =>
            {
                foreach (var messageModule in config.MessageModules)
                {
                    Type module = messageModule;
                    c.For(typeof(IMessageModule)).Singleton().Use(module).Named(typeof(IMessageModule).FullName);
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
            var busConfig = (RhinoServiceBusConfiguration) config;
            container.Configure(c =>
            {
                c.For<IDeploymentAction>().Use<CreateQueuesAction>();
                c.For<IStartableServiceBus>().Singleton().Use<DefaultServiceBus>()
                    .Ctor<MessageOwner[]>().Is(busConfig.MessageOwners.ToArray());
                c.Forward<IStartableServiceBus, IStartable>();
                c.Forward<IStartableServiceBus, IServiceBus>();
            });
        }

        public void RegisterPrimaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration) config;
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
            var loadBalancerConfig = (LoadBalancerConfiguration) config;
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
            var loadBalancerConfig = (LoadBalancerConfiguration) config;
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
            c.For<MessageLoggingModule>().Singleton().Use<MessageLoggingModule>()
              .Ctor<Uri>().Is(logEndpoint);
            c.For<IDeploymentAction>().Use<CreateLogQueueAction>();
          });
        }

        public void RegisterMsmqTransport(Type queueStrategyType)
        {
            container.Configure(c =>
            {
                c.For(typeof (IQueueStrategy)).Singleton().Use(queueStrategyType)
                    .Child("endpoint").Is(config.Endpoint);
                c.For<IMessageBuilder<Message>>().Singleton().Use<MsmqMessageBuilder>();
                c.For<IMsmqTransportAction>().Singleton().Use<ErrorAction>()
                    .Ctor<int>("numberOfRetries").Is(config.NumberOfRetries);
                c.For<ISubscriptionStorage>().Singleton().Use<MsmqSubscriptionStorage>()
                    .Ctor<Uri>().Is(config.Endpoint);
                c.For<ITransport>().Singleton().Use<MsmqTransport>()
                    .Ctor<int>("threadCount").Is(config.ThreadCount)
                    .Ctor<Uri>().Is(config.Endpoint)
                    .Ctor<IsolationLevel>().Is(config.IsolationLevel)
                    .Ctor<TransactionalOptions>().Is(config.Transactional)
                    .Ctor<bool>().Is(config.ConsumeInTransaction);
                c.Scan(s =>
                {
                    s.Assembly(typeof(IMsmqTransportAction).Assembly);
                    s.With(new SingletonConvention<IMsmqTransportAction>());
                    s.AddAllTypesOf<IMsmqTransportAction>();
                    s.Exclude(t => t == typeof(ErrorAction));
                });
            });
        }

        public void RegisterQueueCreation()
        {
            container.Configure(c => c.For<QueueCreationModule>().Singleton().Use<QueueCreationModule>());
        }

        public void RegisterMsmqOneWay()
        {
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration) config;
            container.Configure(c =>
            {
                c.For<IMessageBuilder<Message>>().Singleton().Use<MsmqMessageBuilder>();
                c.For<IOnewayBus>().Singleton().Use<MsmqOnewayBus>()
                    .Ctor<MessageOwner[]>().Is(oneWayConfig.MessageOwners);
            });
        }

        public void RegisterRhinoQueuesTransport()
        {
            container.Configure(c =>
            {
                var busConfig = config.ConfigurationSection.Bus;
                c.For<ISubscriptionStorage>().Singleton().Use<PhtSubscriptionStorage>()
                    .Ctor<string>().Is(busConfig.SubscriptionPath);
                c.For<ITransport>().Singleton().Use<RhinoQueuesTransport>()
                    .Ctor<int>("threadCount").Is(config.ThreadCount)
                    .Ctor<Uri>().Is(config.Endpoint)
                    .Ctor<IsolationLevel>().Is(config.IsolationLevel)
                    .Ctor<int>("numberOfRetries").Is(config.NumberOfRetries)
                    .Ctor<string>().Is(busConfig.QueuePath)
                    .Ctor<bool>().Is(busConfig.EnablePerformanceCounters);
                c.For<IMessageBuilder<MessagePayload>>().Singleton().Use<RhinoQueuesMessageBuilder>();
            });
        }

        public void RegisterRhinoQueuesOneWay()
        {
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration) config;
            var busConfig = config.ConfigurationSection.Bus;

            container.Configure(c =>
            {
                c.For<IMessageBuilder<MessagePayload>>().Singleton().Use<RhinoQueuesMessageBuilder>();
                c.For<IOnewayBus>().Singleton().Use<RhinoQueuesOneWayBus>()
                    .Ctor<MessageOwner[]>().Is(oneWayConfig.MessageOwners)
                    .Ctor<string>().Is(busConfig.QueuePath)
                    .Ctor<bool>().Is(busConfig.EnablePerformanceCounters);
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