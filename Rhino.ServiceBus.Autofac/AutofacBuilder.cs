using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using Autofac;
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
            var builder = new ContainerBuilder();
            builder.RegisterModule(new ConsumerInterceptorModule(interceptor));
            builder.Update(container);
        }

        public void RegisterDefaultServices()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(container);
            builder.RegisterType<AutofacServiceLocator>()
                .As<IServiceLocator>()
                .SingleInstance();
            builder.RegisterAssemblyTypes(typeof(IServiceBus).Assembly)
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
            foreach(var module in config.MessageModules)
            {
                builder.RegisterType(module)
                    .Named<string>(module.FullName)
                    .As(typeof(IMessageModule))
                    .SingleInstance();
            }
            builder.Update(container);

            foreach(var busConfigurationAware in container.Resolve<IEnumerable<IBusConfigurationAware>>())
                busConfigurationAware.Configure(config, this);
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

        public void RegisterMsmqTransport(Type queueStrategyType)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType(queueStrategyType)
                .WithParameter("endpoint", config.Endpoint)
                .As<IQueueStrategy>()
                .SingleInstance();
            builder.RegisterType<MsmqMessageBuilder>()
                .As<IMessageBuilder<Message>>()
                .SingleInstance();
            builder.RegisterType<ErrorAction>()
                .WithParameter("numberOfRetries", config.NumberOfRetries)
                .As<IMsmqTransportAction>()
                .SingleInstance();
            builder.RegisterType<MsmqSubscriptionStorage>()
                .WithParameter("queueBusListensTo", config.Endpoint)
                .As<ISubscriptionStorage>()
                .SingleInstance();
            builder.RegisterType<MsmqTransport>()
                .WithParameter("endpoint", config.Endpoint)
                .WithParameter("threadCount", config.ThreadCount)
                .WithParameter("queueIsolationLevel", config.IsolationLevel)
                .WithParameter("transactional", config.Transactional)
                .WithParameter("consumeInTransaction", config.ConsumeInTransaction)
                .As<ITransport>()
                .SingleInstance();
            builder.RegisterAssemblyTypes(typeof(IMsmqTransportAction).Assembly)
                .Where(x => typeof(IMsmqTransportAction).IsAssignableFrom(x) && x != typeof(ErrorAction))
                .As<IMsmqTransportAction>()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterQueueCreation()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<QueueCreationModule>()
                .AsSelf()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterMsmqOneWay()
        {
            var builder = new ContainerBuilder();
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration)config;
            builder.RegisterType<MsmqMessageBuilder>()
                .As<IMessageBuilder<Message>>()
                .SingleInstance();
            builder.RegisterType<MsmqOnewayBus>()
                .WithParameter("messageOwners", oneWayConfig.MessageOwners)
                .As<IOnewayBus>()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterRhinoQueuesTransport()
        {
            var busConfig = config.ConfigurationSection.Bus;
            var builder = new ContainerBuilder();
            builder.RegisterType<PhtSubscriptionStorage>()
                .WithParameter("subscriptionPath", busConfig.SubscriptionPath)
                .As<ISubscriptionStorage>()
                .SingleInstance();
            builder.RegisterType<RhinoQueuesTransport>()
                .WithParameter("threadCount", config.ThreadCount)
                .WithParameter("endpoint", config.Endpoint)
                .WithParameter("queueIsolationLevel", config.IsolationLevel)
                .WithParameter("numberOfRetries", config.NumberOfRetries)
                .WithParameter("path", busConfig.QueuePath)
                .WithParameter("enablePerformanceCounters", busConfig.EnablePerformanceCounters)
                .As<ITransport>()
                .SingleInstance();
            builder.RegisterType<RhinoQueuesMessageBuilder>()
                .As<IMessageBuilder<MessagePayload>>()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterRhinoQueuesOneWay()
        {
            var builder = new ContainerBuilder();
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration)config;
            var busConfig = config.ConfigurationSection.Bus;
            builder.RegisterType<RhinoQueuesMessageBuilder>()
                .As<IMessageBuilder<MessagePayload>>()
                .SingleInstance();
            builder.RegisterType<RhinoQueuesOneWayBus>()
                .WithParameter("messageOwners", oneWayConfig.MessageOwners)
                .WithParameter("path", busConfig.QueuePath)
                .WithParameter("enablePerformanceCounters", busConfig.EnablePerformanceCounters)
                .As<IOnewayBus>()
                .SingleInstance();
            builder.Update(container);
        }

        public void RegisterSecurity(byte[] key)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<RijndaelEncryptionService>()
                .WithParameter(new NamedParameter("key", key))
                .As<IEncryptionService>().SingleInstance();
            builder.RegisterType<WireEcryptedStringConvertor>()
                .As<IValueConvertor<WireEcryptedString>>()
                .SingleInstance();
            builder.RegisterType<WireEncryptedMessageConvertor>()
                .As<IElementSerializationBehavior>().SingleInstance();
            builder.Update(container);
        }

        public void RegisterNoSecurity()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ThrowingWireEcryptedStringConvertor>()
                .As<IValueConvertor<WireEcryptedString>>().SingleInstance();
            builder.RegisterType<ThrowingWireEncryptedMessageConvertor>()
                .As<IElementSerializationBehavior>().SingleInstance();
            builder.Update(container);
        }
    }
}