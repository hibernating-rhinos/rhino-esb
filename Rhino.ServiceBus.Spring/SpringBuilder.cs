using System;
using System.IO;
using System.Linq;
using System.Messaging;

using Rhino.Queues;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Convertors;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Msmq.TransportActions;
using Rhino.ServiceBus.RhinoQueues;

using Spring.Context;

using ErrorAction = Rhino.ServiceBus.Msmq.TransportActions.ErrorAction;
using LoadBalancerConfiguration = Rhino.ServiceBus.LoadBalancer.LoadBalancerConfiguration;

namespace Rhino.ServiceBus.Spring
{
    public class SpringBuilder : IBusContainerBuilder
    {
        private readonly AbstractRhinoServiceBusConfiguration config;
        private readonly IConfigurableApplicationContext applicationContext;

        public SpringBuilder(AbstractRhinoServiceBusConfiguration config, IConfigurableApplicationContext applicationContext)
        {
            this.config = config;
            this.applicationContext = applicationContext;
            config.BuildWith(this);
        }

        public void WithInterceptor(IConsumerInterceptor interceptor)
        {
            applicationContext.ObjectFactory.AddObjectPostProcessor(new ConsumerInterceptor(interceptor, applicationContext));
        }

        public void RegisterDefaultServices()
        {
            applicationContext.RegisterSingleton<IServiceLocator>(() => new SpringServiceLocator(applicationContext));

            applicationContext.RegisterSingletons<IBusConfigurationAware>(typeof (IServiceBus).Assembly);

            applicationContext.RegisterSingleton<IReflection>(() => new DefaultReflection());

            applicationContext.RegisterSingleton(config.SerializerType);

            applicationContext.RegisterSingleton<IEndpointRouter>(() => new EndpointRouter());

            foreach (var module in config.MessageModules)
            {
                applicationContext.RegisterSingleton(module);
            }

            foreach (var busConfigurationAware in applicationContext.GetAll<IBusConfigurationAware>())
            {
                busConfigurationAware.Configure(config, this);
            }
        }

        public void RegisterBus()
        {
            var busConfig = (RhinoServiceBusConfiguration) config;

            var serviceBus = new DefaultServiceBus(applicationContext.Get<IServiceLocator>(),
                                                   applicationContext.Get<ITransport>(),
                                                   applicationContext.Get<ISubscriptionStorage>(),
                                                   applicationContext.Get<IReflection>(),
                                                   applicationContext.GetAll<IMessageModule>().ToArray(),
                                                   busConfig.MessageOwners.ToArray(),
                                                   applicationContext.Get<IEndpointRouter>());

            applicationContext.RegisterSingleton<IStartableServiceBus>(() => serviceBus);
            applicationContext.RegisterSingleton<IStartable>(() => serviceBus);

            applicationContext.RegisterSingleton(() => new CreateLogQueueAction(applicationContext.Get<MessageLoggingModule>(), applicationContext.Get<ITransport>()));
            applicationContext.RegisterSingleton(() => new CreateQueuesAction(applicationContext.Get<IQueueStrategy>(), applicationContext.Get<IServiceBus>()));
        }

        public void RegisterPrimaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration) config;

            applicationContext.RegisterSingleton(() => new MsmqLoadBalancer(applicationContext.Get<IMessageSerializer>(),
                                                                            applicationContext.Get<IQueueStrategy>(),
                                                                            applicationContext.Get<IEndpointRouter>(),
                                                                            loadBalancerConfig.Endpoint,
                                                                            loadBalancerConfig.ThreadCount,
                                                                            loadBalancerConfig.Transactional,
                                                                            applicationContext.Get<IMessageBuilder<Message>>()));

            applicationContext.RegisterSingleton(() => new CreateLoadBalancerQueuesAction(applicationContext.Get<IQueueStrategy>(), applicationContext.Get<MsmqLoadBalancer>()));
        }

        public void RegisterSecondaryLoadBalancer()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration) config;

            applicationContext.RegisterSingleton(() => new MsmqSecondaryLoadBalancer(applicationContext.Get<IMessageSerializer>(),
                                                                                     applicationContext.Get<IQueueStrategy>(),
                                                                                     applicationContext.Get<IEndpointRouter>(),
                                                                                     loadBalancerConfig.Endpoint,
                                                                                     loadBalancerConfig.PrimaryLoadBalancer,
                                                                                     loadBalancerConfig.ThreadCount,
                                                                                     loadBalancerConfig.Transactional,
                                                                                     applicationContext.Get<IMessageBuilder<Message>>()));

            applicationContext.RegisterSingleton(() => new CreateLoadBalancerQueuesAction(applicationContext.Get<IQueueStrategy>(), applicationContext.Get<MsmqLoadBalancer>()));
        }

        public void RegisterReadyForWork()
        {
            var loadBalancerConfig = (LoadBalancerConfiguration) config;

            applicationContext.RegisterSingleton(() => new MsmqReadyForWorkListener(applicationContext.Get<IQueueStrategy>(),
                                                                                    loadBalancerConfig.ReadyForWork,
                                                                                    loadBalancerConfig.ThreadCount,
                                                                                    applicationContext.Get<IMessageSerializer>(),
                                                                                    applicationContext.Get<IEndpointRouter>(),
                                                                                    loadBalancerConfig.Transactional,
                                                                                    applicationContext.Get<IMessageBuilder<Message>>()));

            applicationContext.RegisterSingleton(() => new CreateReadyForWorkQueuesAction(applicationContext.Get<IQueueStrategy>(), applicationContext.Get<MsmqReadyForWorkListener>()));
        }

        public void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint)
        {
            applicationContext.RegisterSingleton(typeof (LoadBalancerMessageModule).FullName, () => new LoadBalancerMessageModule(loadBalancerEndpoint, applicationContext.Get<IEndpointRouter>()));
        }

        public void RegisterLoggingEndpoint(Uri logEndpoint)
        {
            applicationContext.RegisterSingleton(typeof (MessageLoggingModule).FullName, () => new MessageLoggingModule(applicationContext.Get<IEndpointRouter>(), logEndpoint));
        }

        public void RegisterMsmqTransport(Type queueStrategyType)
        {
            if (queueStrategyType.GetConstructor(new[] {typeof (IQueueStrategy), typeof (Uri)}) != null)
            {
                applicationContext.RegisterSingleton(queueStrategyType, typeof (IQueueStrategy).FullName, applicationContext.Get<IEndpointRouter>(), config.Endpoint);
            }
            else
            {
                // use default
                applicationContext.RegisterSingleton(queueStrategyType);
            }

            applicationContext.RegisterSingleton(() => new MsmqMessageBuilder(
                                                           applicationContext.Get<IMessageSerializer>(),
                                                           applicationContext.Get<IServiceLocator>()));

            applicationContext.RegisterSingleton(() => new ErrorAction(
                                                           config.NumberOfRetries,
                                                           applicationContext.Get<IQueueStrategy>()));

            applicationContext.RegisterSingleton(() => new MsmqSubscriptionStorage(
                                                           applicationContext.Get<IReflection>(),
                                                           applicationContext.Get<IMessageSerializer>(),
                                                           config.Endpoint,
                                                           applicationContext.Get<IEndpointRouter>(),
                                                           applicationContext.Get<IQueueStrategy>()));

            applicationContext.RegisterSingleton<ITransport>(typeof (MsmqTransport).FullName, () => new MsmqTransport(
                                                                                                        applicationContext.Get<IMessageSerializer>(),
                                                                                                        applicationContext.Get<IQueueStrategy>(),
                                                                                                        config.Endpoint,
                                                                                                        config.ThreadCount,
                                                                                                        applicationContext.GetAll<IMsmqTransportAction>().ToArray(),
                                                                                                        applicationContext.Get<IEndpointRouter>(),
                                                                                                        config.IsolationLevel, config.Transactional, config.ConsumeInTransaction,
                                                                                                        applicationContext.Get<IMessageBuilder<Message>>()));

            typeof (IMsmqTransportAction).Assembly.GetTypes()
                .Where(x => typeof (IMsmqTransportAction).IsAssignableFrom(x) && x != typeof (ErrorAction) && !x.IsAbstract && !x.IsInterface)
                .ToList()
                .ForEach(x => applicationContext.RegisterSingleton(x, x.FullName));
        }

        public void RegisterQueueCreation()
        {
            applicationContext.RegisterSingleton(() => new QueueCreationModule(applicationContext.Get<IQueueStrategy>()));
        }

        public void RegisterMsmqOneWay()
        {
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration) config;

            applicationContext.RegisterSingleton(() => new MsmqMessageBuilder(applicationContext.Get<IMessageSerializer>(), applicationContext.Get<IServiceLocator>()));
            applicationContext.RegisterSingleton(() => new MsmqOnewayBus(oneWayConfig.MessageOwners, applicationContext.Get<IMessageBuilder<Message>>()));
        }

        public void RegisterRhinoQueuesTransport(string path, string name)
        {
            applicationContext.RegisterSingleton(() => new PhtSubscriptionStorage(Path.Combine(path, name + "_subscriptions.esent"),
                                                                                  applicationContext.Get<IMessageSerializer>(),
                                                                                  applicationContext.Get<IReflection>()));

            applicationContext.RegisterSingleton<ITransport>(typeof (RhinoQueuesTransport).FullName, () => new RhinoQueuesTransport(config.Endpoint,
                                                                                                                                    applicationContext.Get<IEndpointRouter>(),
                                                                                                                                    applicationContext.Get<IMessageSerializer>(),
                                                                                                                                    config.ThreadCount,
                                                                                                                                    Path.Combine(path, name + ".esent"),
                                                                                                                                    config.IsolationLevel,
                                                                                                                                    config.NumberOfRetries,
                                                                                                                                    applicationContext.Get<IMessageBuilder<MessagePayload>>()));

            applicationContext.RegisterSingleton(() => new RhinoQueuesMessageBuilder(applicationContext.Get<IMessageSerializer>()));
        }

        public void RegisterRhinoQueuesOneWay()
        {
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration) config;

            applicationContext.RegisterSingleton(() => new RhinoQueuesMessageBuilder(applicationContext.Get<IMessageSerializer>()));
            applicationContext.RegisterSingleton(() => new RhinoQueuesOneWayBus(oneWayConfig.MessageOwners, applicationContext.Get<IMessageSerializer>(), applicationContext.Get<IMessageBuilder<MessagePayload>>()));
        }

        public void RegisterSecurity(byte[] key)
        {
            applicationContext.RegisterSingleton(() => new RijndaelEncryptionService(key));
            applicationContext.RegisterSingleton(() => new WireEcryptedStringConvertor(applicationContext.Get<IEncryptionService>()));
            applicationContext.RegisterSingleton(() => new WireEncryptedMessageConvertor(applicationContext.Get<IEncryptionService>()));
        }

        public void RegisterNoSecurity()
        {
            applicationContext.RegisterSingleton(() => new ThrowingWireEcryptedStringConvertor());
            applicationContext.RegisterSingleton(() => new ThrowingWireEncryptedMessageConvertor());
        }
    }
}