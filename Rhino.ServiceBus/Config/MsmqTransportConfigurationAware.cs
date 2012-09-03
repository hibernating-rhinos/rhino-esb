using System;
using System.Linq;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Internal;
using System.Messaging;
using Rhino.ServiceBus.Msmq.TransportActions;

namespace Rhino.ServiceBus.Config
{
    public class MsmqTransportConfigurationAware : IBusConfigurationAware
    {
        private Type queueStrategyImpl = typeof(SubQueueStrategy);

        public MsmqTransportConfigurationAware()
        {
            DetectQueueStrategy();
        }

        /// <summary>
        /// Detects the valid queue strategy automatically.
        /// </summary>
        private void DetectQueueStrategy()
        {
            if (Environment.OSVersion.Version.Major <= 5)
                queueStrategyImpl = typeof(FlatQueueStrategy);
        }

        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder, IServiceLocator locator)
        {
            if (!(config is RhinoServiceBusConfiguration) && !(config is LoadBalancer.LoadBalancerConfiguration))
                return;

            if (!config.Endpoint.Scheme.Equals("msmq", StringComparison.InvariantCultureIgnoreCase))
                return;

            if (config.UseFlatQueue)
                queueStrategyImpl = typeof(FlatQueueStrategy);

            if (!config.DisableAutoQueueCreation)
                RegisterQueueCreation(builder, locator);

            RegisterMsmqTransport(config, builder, locator, queueStrategyImpl);
        }

        private void RegisterQueueCreation(IBusContainerBuilder b, IServiceLocator l)
        {
            b.RegisterSingleton<IServiceBusAware>(Guid.NewGuid().ToString(), () => (IServiceBusAware)new QueueCreationModule(
                l.Resolve<IQueueStrategy>()));
        }

        private void RegisterMsmqTransport(AbstractRhinoServiceBusConfiguration c, IBusContainerBuilder b, IServiceLocator l, Type queueStrategyType)
        {
            if (queueStrategyType == typeof(FlatQueueStrategy))
                b.RegisterSingleton<IQueueStrategy>(() => (IQueueStrategy)new FlatQueueStrategy(
                    l.Resolve<IEndpointRouter>(),
                    c.Endpoint));
            else
                b.RegisterSingleton<IQueueStrategy>(() => (IQueueStrategy)new SubQueueStrategy());

            b.RegisterSingleton<IMessageBuilder<Message>>(() => (IMessageBuilder<Message>)new MsmqMessageBuilder(
                l.Resolve<IMessageSerializer>(),
                l.Resolve<IServiceLocator>()));

            b.RegisterSingleton<IMsmqTransportAction>(Guid.NewGuid().ToString(), () => (IMsmqTransportAction)new ErrorAction(
                c.NumberOfRetries,
                l.Resolve<IQueueStrategy>()));

            b.RegisterSingleton<ISubscriptionStorage>(() => (ISubscriptionStorage)new MsmqSubscriptionStorage(
                l.Resolve<IReflection>(),
                l.Resolve<IMessageSerializer>(),
                c.Endpoint,
                l.Resolve<IEndpointRouter>(),
                l.Resolve<IQueueStrategy>()));

            b.RegisterSingleton<ITransport>(() => (ITransport)new MsmqTransport(
                l.Resolve<IMessageSerializer>(),
                l.Resolve<IQueueStrategy>(),
                c.Endpoint,
                c.ThreadCount,
                l.ResolveAll<IMsmqTransportAction>().ToArray(),
                l.Resolve<IEndpointRouter>(),
                c.IsolationLevel,
                c.Transactional,
                c.ConsumeInTransaction,
                l.Resolve<IMessageBuilder<Message>>()));

            b.RegisterAll<IMsmqTransportAction>(typeof(ErrorAction));
        }
    }
}