using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Msmq.TransportActions;
using Rhino.ServiceBus.Sagas;
using Rhino.ServiceBus.Serializers;

namespace Rhino.ServiceBus.Impl
{
    public abstract class AbstractRhinoServiceBusFacility : AbstractFacility
    {
        protected readonly List<Type> messageModules = new List<Type>();
        private Type serializerImpl = typeof(XmlMessageSerializer);
        private readonly Type transportImpl = typeof(MsmqTransport);
        protected Uri endpoint;
        protected int numberOfRetries = 5;
        private readonly Type subscriptionStorageImpl = typeof(MsmqSubscriptionStorage);
        protected int threadCount = 1;
        private Type queueStrategyImpl = typeof(SubQueueStrategy);
        private bool useCreationModule = true;

        protected AbstractRhinoServiceBusFacility()
        {
            DetectQueueStrategy();
        }

        /// <summary>
        /// Detects the valid queue strategy automatically.
        /// </summary>
        private void DetectQueueStrategy()
        {
            if (Environment.OSVersion.Version.Major <= 5)
            {
                queueStrategyImpl = typeof(FlatQueueStrategy);
            }
        }

        public AbstractRhinoServiceBusFacility AddMessageModule<TModule>()
            where TModule : IMessageModule
        {
            messageModules.Add(typeof(TModule));
            return this;
        }

        public AbstractRhinoServiceBusFacility InsertMessageModuleAtFirst<TModule>()
            where TModule : IMessageModule
        {
            messageModules.Insert(0, typeof (TModule));
            return this;
        }

        /// <summary>
        /// Implementation for MSMQ 3.0. 
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <listheader>Queue structure requirements, where <c>my_root_queue</c> is the endpoint:</listheader>
        /// <item>my_root_queue</item>
        /// <item>my_root_queue<c>#subscriptions</c></item>
        /// <item>my_root_queue<c>#errors</c></item>
        /// <item>my_root_queue<c>#discarded</c></item>
        /// <item>my_root_queue<c>#timeout</c></item>
        /// </list>
        /// </remarks>
        /// <returns></returns>
        public AbstractRhinoServiceBusFacility UseFlatQueueStructure()
        {
            queueStrategyImpl = typeof(FlatQueueStrategy);
            return this;
        }

        /// <summary>
        /// <c>Default</c> - <b>For MSMQ 4.0 only</b>. Only a single physical queue is required.
        /// </summary>
        /// <returns></returns>
        public AbstractRhinoServiceBusFacility UseSubqueuesQueueStructure()
        {
            queueStrategyImpl = typeof(SubQueueStrategy);
            return this;
        }

        /// <summary>
        /// Disables the queue auto creation module
        /// </summary>
        /// <remarks>
        /// <para>By default, the
        /// <see cref="QueueCreationModule"/> will create queue(s) automagically when the bus starts.</para>
        /// </remarks>
        /// <returns></returns>
        public AbstractRhinoServiceBusFacility DisableQueueAutoCreation()
        {
            useCreationModule = false;
            return this;
        }

        protected override void Init()
        {
            if(FacilityConfig==null)
                throw new ConfigurationErrorsException(
                    "could not find facility configuration section with the same name of the facility");

            Kernel.ComponentModelCreated += Kernel_OnComponentModelCreated;
            Kernel.Resolver.AddSubResolver(new ArrayResolver(Kernel));

            ReadConfiguration();

            Kernel.Register(
                AllTypes.Of<IBusConfigurationAware>()
                    .FromAssembly(typeof(IBusConfigurationAware).Assembly)
                );

            foreach (var configurationAware in Kernel.ResolveAll<IBusConfigurationAware>())
            {
                configurationAware.Configure(this, FacilityConfig);
            }
            
            foreach (var type in messageModules)
            {
                if (Kernel.HasComponent(type) == false)
                    Kernel.AddComponent(type.FullName, type);
            }

            if (useCreationModule)
            {
                Kernel.Register(Component.For<QueueCreationModule>());
            }

            RegisterComponents();

            Kernel.Register(
                Component.For<IReflection>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy<DefaultReflection>(),
                Component.For<IQueueStrategy>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(queueStrategyImpl).DependsOn(new { endpoint }),
                Component.For<ISubscriptionStorage>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(subscriptionStorageImpl)
                    .DependsOn(new
                    {
                        queueBusListensTo = endpoint
                    }),
                Component.For<ITransport>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(transportImpl)
                    .DependsOn(new
                    {
                        threadCount,
                        endpoint,
                    }),
                Component.For<IMessageSerializer>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(serializerImpl)
                );

            Kernel.Register(
                Component.For<IEndpointRouter>()
                    .ImplementedBy<EndpointRouter>(),
                Component.For<ITransportAction>()
                    .ImplementedBy<ErrorAction>()
                    .DependsOn(new { numberOfRetries }),
                AllTypes.Of<ITransportAction>()
                    .FromAssembly(typeof(ITransportAction).Assembly)
                    .Unless(x => x == typeof(ErrorAction))
                    .WithService.FirstInterface()
                    .Configure(registration =>
                               registration.LifeStyle.Is(LifestyleType.Singleton))
                );
        }

        protected abstract void RegisterComponents();

        protected abstract void ReadConfiguration();

        private static void Kernel_OnComponentModelCreated(ComponentModel model)
        {
            if (typeof(IMessageConsumer).IsAssignableFrom(model.Implementation) == false)
                return;

            var interfaces = model.Implementation.GetInterfaces()
                .Where(x => x.IsGenericType && x.IsGenericTypeDefinition == false)
                .Select(x => x.GetGenericTypeDefinition())
                .ToList();

            if (interfaces.Contains(typeof(InitiatedBy<>)) &&
                interfaces.Contains(typeof(ISaga<>)) == false)
            {
                throw new InvalidUsageException("Message consumer: " + model.Implementation + " implements InitiatedBy<TMsg> but doesn't implment ISaga<TState>. " + Environment.NewLine +
                                                "Did you forget to inherit from ISaga<TState> ?");
            }

            if (interfaces.Contains(typeof(InitiatedBy<>)) == false &&
                interfaces.Contains(typeof(Orchestrates<>)))
            {
                throw new InvalidUsageException("Message consumer: " + model.Implementation + " implements Orchestrates<TMsg> but doesn't implment InitiatedBy<TState>. " + Environment.NewLine +
                                                "Did you forget to inherit from InitiatedBy<TState> ?");
            }

            model.LifestyleType = LifestyleType.Transient;
        }

        public void UseMessageSerializer<TMessageSerializer>()
        {
            serializerImpl = typeof(TMessageSerializer);
        }
    }
}