using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.Core;
using Castle.Core.Configuration;
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
	using Castle.MicroKernel;

	public class RhinoServiceBusFacility : AbstractFacility
    {
        private readonly List<Type> messageModules = new List<Type>();
        private readonly List<MessageOwner> messageOwners = new List<MessageOwner>();
        private Type serializerImpl = typeof(XmlMessageSerializer);
        private readonly Type transportImpl = typeof(MsmqTransport);
        private Uri endpoint;
        private int numberOfRetries = 5;
        private readonly Type subscriptionStorageImpl = typeof(MsmqSubscriptionStorage);
        private int threadCount = 1;
        private Type queueStrategyImpl = typeof(SubQueueStrategy);
        private bool useCreationModule = true;

    	public RhinoServiceBusFacility()
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

        public RhinoServiceBusFacility AddMessageModule<TModule>()
            where TModule : IMessageModule
        {
            messageModules.Add(typeof(TModule));
            return this;
        }

        public RhinoServiceBusFacility InsertMessageModuleAtFirst<TModule>()
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
        public RhinoServiceBusFacility UseFlatQueueStructure()
        {
            queueStrategyImpl = typeof(FlatQueueStrategy);
            return this;
        }

        /// <summary>
        /// <c>Default</c> - <b>For MSMQ 4.0 only</b>. Only a single physical queue is required.
        /// </summary>
        /// <returns></returns>
        public RhinoServiceBusFacility UseSubqueuesQueueStructure()
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
        public RhinoServiceBusFacility DisableQueueAutoCreation()
        {
            useCreationModule = false;
            return this;
        }
        protected override void Init()
        {
            Kernel.ComponentModelCreated += Kernel_OnComponentModelCreated;
            Kernel.Resolver.AddSubResolver(new ArrayResolver(Kernel));

            ReadBusConfiguration();
            ReadMessageOwners();

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

            Kernel.Register(
                Component.For<IServiceBus, IStartableServiceBus>()
                    .ImplementedBy<DefaultServiceBus>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .DependsOn(new
                    {
                        messageOwners = messageOwners.ToArray(),
                    })
                    .Parameters(Parameter.ForKey("modules")
                                    .Eq(CreateModuleConfigurationNode())
                    ),
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


        private void ReadMessageOwners()
        {
            IConfiguration messageConfig = FacilityConfig.Children["messages"];
            if (messageConfig == null)
                throw new ConfigurationErrorsException("Could not find 'messages' node in confiuration");

            foreach (IConfiguration configuration in messageConfig.Children)
            {
                if (configuration.Name != "add")
                    throw new ConfigurationErrorsException("Unknown node 'messages/" + configuration.Name + "'");

                string msgName = configuration.Attributes["name"];
                if (string.IsNullOrEmpty(msgName))
                    throw new ConfigurationErrorsException("Invalid name element in the <messages/> element");

                string uriString = configuration.Attributes["endpoint"];
                Uri ownerEndpoint;
                try
                {
                    ownerEndpoint = new Uri(uriString);
                }
                catch (Exception e)
                {
                    throw new ConfigurationErrorsException("Invalid endpoint url: " + uriString, e);
                }

                messageOwners.Add(new MessageOwner
                {
                    Name = msgName,
                    Endpoint = ownerEndpoint
                });
            }
        }

        private IConfiguration CreateModuleConfigurationNode()
        {
            var config = new MutableConfiguration("array");
            foreach (Type type in messageModules)
            {
                config.CreateChild("item", "${" + type.FullName + "}");
            }
            return config;
        }


        private void ReadBusConfiguration()
        {
            IConfiguration busConfig = FacilityConfig.Children["bus"];
            if (busConfig == null)
                throw new ConfigurationErrorsException("Could not find 'bus' node in confiuration");

            string retries = busConfig.Attributes["numberOfRetries"];
            int result;
            if (int.TryParse(retries, out result))
                numberOfRetries = result;

            string threads = busConfig.Attributes["threadCounts"];
            if (int.TryParse(threads, out result))
                threadCount = result;

            string uriString = busConfig.Attributes["endpoint"];
            if (Uri.TryCreate(uriString, UriKind.Absolute, out endpoint) == false)
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'endpoint' on 'bus' has an invalid value '" + uriString + "'");
            }
        }

    	public void UseMessageSerializer<TMessageSerializer>()
    	{
			serializerImpl = typeof(TMessageSerializer);
    	}
    }
}
