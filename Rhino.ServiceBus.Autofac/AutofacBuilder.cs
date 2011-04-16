using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using Autofac;
using Autofac.Core;
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
using LoadBalancerConfiguration = Rhino.ServiceBus.LoadBalancer.LoadBalancerConfiguration;

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
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new ConsumerModule(interceptor));
            containerBuilder.Update(container);
        }

        public void RegisterDefaultServices()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(container);
            containerBuilder.RegisterType<AutofacServiceLocator>().As<IServiceLocator>();
            containerBuilder.RegisterAssemblyTypes(typeof (IServiceBus).Assembly)
                .AssignableTo<IBusConfigurationAware>().As<IBusConfigurationAware>();

            containerBuilder.Update(container);

            foreach (var busConfigurationAware in container.Resolve<IEnumerable<IBusConfigurationAware>>())
            {
                busConfigurationAware.Configure(config, this);
            }

            foreach (var module in config.MessageModules)
            {
                containerBuilder.RegisterType(module).Named<string>(module.FullName).As(typeof (IMessageModule));
            }

            containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<DefaultReflection>().As<IReflection>().SingleInstance();
            containerBuilder.RegisterType(config.SerializerType).As<IMessageSerializer>().SingleInstance();
            containerBuilder.RegisterType<EndpointRouter>().As<IEndpointRouter>().SingleInstance();
            containerBuilder.Update(container);
        }

        public void RegisterBus()
        {
            var containerBuilder = new ContainerBuilder();
            var busConfig = (RhinoServiceBusConfiguration) config;
            containerBuilder.Register(c =>
                                      new DefaultServiceBus(
                                          c.Resolve<IServiceLocator>(),
                                          c.Resolve<ITransport>(),
                                          c.Resolve<ISubscriptionStorage>(),
                                          c.Resolve<IReflection>(),
                                          c.Resolve<IEnumerable<IMessageModule>>().ToArray(),
                                          busConfig.MessageOwners.ToArray(),
                                          c.Resolve<IEndpointRouter>()
                                          ))
                .AsImplementedInterfaces()
                .SingleInstance();

            containerBuilder.RegisterType<CreateLogQueueAction>().As<IDeploymentAction>();
            containerBuilder.RegisterType<CreateQueuesAction>().As<IDeploymentAction>();
            containerBuilder.Update(container);
        }

        public void RegisterPrimaryLoadBalancer()
        {
            var containerBuilder = new ContainerBuilder();
            var loadBalancerConfig = (LoadBalancerConfiguration) config;
            containerBuilder.Register(c => new MsmqLoadBalancer(c.Resolve<IMessageSerializer>(),
                                                                c.Resolve<IQueueStrategy>(),
                                                                c.Resolve<IEndpointRouter>(),
                                                                loadBalancerConfig.Endpoint,
                                                                loadBalancerConfig.ThreadCount,
                                                                loadBalancerConfig.Endpoint,
                                                                loadBalancerConfig.Transactional,
                                                                c.Resolve<IMessageBuilder<Message>>()))
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance();
            containerBuilder.RegisterType<CreateLoadBalancerQueuesAction>().As<IDeploymentAction>();
            containerBuilder.Update(container);
        }

        public void RegisterSecondaryLoadBalancer()
        {
            var containerBuilder = new ContainerBuilder();
            var loadBalancerConfig = (LoadBalancerConfiguration) config;
            containerBuilder.Register(c => new MsmqSecondaryLoadBalancer(c.Resolve<IMessageSerializer>(),
                                                                         c.Resolve<IQueueStrategy>(),
                                                                         c.Resolve<IEndpointRouter>(),
                                                                         loadBalancerConfig.Endpoint,
                                                                         loadBalancerConfig.PrimaryLoadBalancer,
                                                                         loadBalancerConfig.ThreadCount,
                                                                         loadBalancerConfig.Transactional,
                                                                         c.Resolve<IMessageBuilder<Message>>()))
                .As<MsmqLoadBalancer>()
                .AsImplementedInterfaces()
                .SingleInstance();
            containerBuilder.RegisterType<CreateLoadBalancerQueuesAction>().As<IDeploymentAction>();
            containerBuilder.Update(container);
        }

        public void RegisterReadyForWork()
        {
            var containerBuilder = new ContainerBuilder();
            var loadBalancerConfig = (LoadBalancerConfiguration) config;
            containerBuilder.Register(c => new MsmqReadyForWorkListener(c.Resolve<IQueueStrategy>(),
                                                                        loadBalancerConfig.ReadyForWork,
                                                                        loadBalancerConfig.ThreadCount,
                                                                        c.Resolve<IMessageSerializer>(),
                                                                        c.Resolve<IEndpointRouter>(),
                                                                        loadBalancerConfig.Transactional,
                                                                        c.Resolve<IMessageBuilder<Message>>()))
                .AsSelf().SingleInstance();
            containerBuilder.RegisterType<CreateReadyForWorkQueuesAction>().As<IDeploymentAction>();
            containerBuilder.Update(container);
        }

        public void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.Register(c => new LoadBalancerMessageModule(loadBalancerEndpoint, c.Resolve<IEndpointRouter>()))
                .AsSelf().SingleInstance();
            containerBuilder.Update(container);
        }

        public void RegisterLoggingEndpoint(Uri logEndpoint)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.Register(c => new MessageLoggingModule(c.Resolve<IEndpointRouter>(), logEndpoint))
                .AsSelf().SingleInstance();
            containerBuilder.Update(container);
        }

        public void RegisterMsmqTransport(Type queueStrategyType)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType(queueStrategyType)
                .WithParameter(new NamedParameter("endpoint", config.Endpoint))
                .WithParameter(new ResolvedParameter((p, c) => p.ParameterType == typeof(IEndpointRouter), (p, c) => c.Resolve<IEndpointRouter>()))
                .As<IQueueStrategy>().SingleInstance();
            containerBuilder.RegisterType<MsmqMessageBuilder>().As<IMessageBuilder<Message>>().SingleInstance();
            containerBuilder.Register(c => new ErrorAction(config.NumberOfRetries, c.Resolve<IQueueStrategy>()))
                .As<IMsmqTransportAction>().SingleInstance();
            containerBuilder.Register(c => new MsmqSubscriptionStorage(c.Resolve<IReflection>(),
                                                                       c.Resolve<IMessageSerializer>(),
                                                                       config.Endpoint,
                                                                       c.Resolve<IEndpointRouter>(),
                                                                       c.Resolve<IQueueStrategy>()))
                .As<ISubscriptionStorage>().SingleInstance();
            containerBuilder.Register(c => new MsmqTransport(c.Resolve<IMessageSerializer>(),
                                                             c.Resolve<IQueueStrategy>(),
                                                             config.Endpoint,
                                                             config.ThreadCount,
                                                             c.Resolve<IEnumerable<IMsmqTransportAction>>().ToArray(),
                                                             c.Resolve<IEndpointRouter>(),
                                                             config.IsolationLevel,
                                                             config.Transactional, config.ConsumeInTransaction,
                                                             c.Resolve<IMessageBuilder<Message>>()))
                .As<ITransport>().SingleInstance();
            containerBuilder.RegisterAssemblyTypes(typeof (IMsmqTransportAction).Assembly)
                .Where(x => typeof(IMsmqTransportAction).IsAssignableFrom(x) && x != typeof(ErrorAction))
                .SingleInstance();
            containerBuilder.Update(container);
        }

        public void RegisterQueueCreation()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<QueueCreationModule>().AsSelf().SingleInstance();
            containerBuilder.Update(container);
        }

        public void RegisterMsmqOneWay()
        {
            var containerBuilder = new ContainerBuilder();
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration) config;
            containerBuilder.RegisterType<MsmqMessageBuilder>().As<IMessageBuilder<Message>>()
                .SingleInstance();
            containerBuilder.RegisterType<MsmqOnewayBus>()
                .WithParameter(new NamedParameter("messageOwners", oneWayConfig.MessageOwners))
                .WithParameter((p, c) => p.Name == "messgeBuilder", (p, c) => c.Resolve<IMessageBuilder<Message>>())
                .As<IOnewayBus>().SingleInstance();
            containerBuilder.Update(container);
        }

        public void RegisterRhinoQueuesTransport(string path, string name)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<PhtSubscriptionStorage>()
                .WithParameter(new NamedParameter("path", Path.Combine(path, name + "_subscriptions.esent")))
                .As<ISubscriptionStorage>().SingleInstance();
            containerBuilder.RegisterType<RhinoQueuesTransport>()
                .WithParameter(new NamedParameter("threadCount", config.ThreadCount))
                .WithParameter(new NamedParameter("endpoint", config.Endpoint))
                .WithParameter(new NamedParameter("queueIsolationLevel", config.IsolationLevel))
                .WithParameter(new NamedParameter("numberOfRetries", config.NumberOfRetries))
                .WithParameter(new NamedParameter("path", Path.Combine(path, name + ".esent")))
                .As<ITransport>().SingleInstance();
            containerBuilder.RegisterType<RhinoQueuesMessageBuilder>()
                .As<IMessageBuilder<MessagePayload>>().
                SingleInstance();
            containerBuilder.Update(container);
        }

        public void RegisterRhinoQueuesOneWay()
        {
            var containerBuilder = new ContainerBuilder();
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration) config;
            containerBuilder.RegisterType<RhinoQueuesMessageBuilder>()
                .As<IMessageBuilder<MessagePayload>>().SingleInstance();
            containerBuilder.RegisterType<RhinoQueuesOneWayBus>()
                .WithParameter(new NamedParameter("messageOwners", oneWayConfig.MessageOwners))
                .WithParameter((p, c) => p.Name == "messageSerializer", (p, c) => c.Resolve<IMessageSerializer>())
                .WithParameter((p, c) => p.Name == "messageBuilder", (p, c) => c.Resolve<IMessageBuilder<MessagePayload>>())
                .As<IOnewayBus>().SingleInstance();
            containerBuilder.Update(container);
        }

        public void RegisterSecurity(byte[] key)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<RijndaelEncryptionService>()
                .WithParameter(new NamedParameter("key", key))
                .As<IEncryptionService>().SingleInstance();
            containerBuilder.RegisterType<WireEcryptedStringConvertor>()
                .As<IValueConvertor<WireEcryptedString>>()
                .SingleInstance();
            containerBuilder.RegisterType<WireEncryptedMessageConvertor>()
                .As<IElementSerializationBehavior>().SingleInstance();
            containerBuilder.Update(container);
        }

        public void RegisterNoSecurity()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<ThrowingWireEcryptedStringConvertor>()
                .As<IValueConvertor<WireEcryptedString>>().SingleInstance();
            containerBuilder.RegisterType<ThrowingWireEncryptedMessageConvertor>()
                .As<IElementSerializationBehavior>().SingleInstance();
            containerBuilder.Update(container);
        }
    }
}