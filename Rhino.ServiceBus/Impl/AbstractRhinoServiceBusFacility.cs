using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Sagas;
using Rhino.ServiceBus.Serializers;
using System.Transactions;

namespace Rhino.ServiceBus.Impl
{
    public abstract class AbstractRhinoServiceBusFacility : AbstractFacility
    {
        protected readonly List<Type> messageModules = new List<Type>();
        private Type serializerImpl = typeof(XmlMessageSerializer);
        protected IsolationLevel queueIsolationLevel = IsolationLevel.Serializable;

        protected AbstractRhinoServiceBusFacility()
        {
            ThreadCount = 1;
            NumberOfRetries = 5;
        }

        public Uri Endpoint { get; set; }

        public int NumberOfRetries { get; set; }

        public int ThreadCount { get; set; }

        public bool UseFlatQueue { get; set; }

        public bool DisableAutoQueueCreation { get; set; }

        public IsolationLevel IsolationLevel
        {
            get { return queueIsolationLevel; }
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

            RegisterComponents();

            Kernel.Register(
                Component.For<IReflection>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy<DefaultReflection>(),
                
                Component.For<IMessageSerializer>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(serializerImpl),
                Component.For<IEndpointRouter>()
                     .ImplementedBy<EndpointRouter>()
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

        public IFacility DisableQueueAutoCreation()
        {
            DisableAutoQueueCreation = true;
            return this;
        }
    }
}