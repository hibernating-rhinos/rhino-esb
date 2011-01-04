using System;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus
{
    public static class Extensions
    {
        public static AbstractRhinoServiceBusFacility UseCastleWindsor(this AbstractRhinoServiceBusFacility configuration)
        {
            return UseCastleWindsor(configuration, new WindsorContainer());
        }

        public static AbstractRhinoServiceBusFacility UseCastleWindsor(this AbstractRhinoServiceBusFacility configuration, IWindsorContainer container)
        {
            configuration.ConfigurationStarted += () => SetupDefaultRegistration(container, configuration);
            return configuration;
        }

        private static void SetupDefaultRegistration(IWindsorContainer container, AbstractRhinoServiceBusFacility configuration)
        {
            if (!container.Kernel.HasComponent(typeof(IWindsorContainer)))
                container.Register(Component.For<IWindsorContainer>().Instance(container));

            container.Register(Component.For<IServiceLocator>().ImplementedBy<CastleServiceLocator>());

            container.Register(
                AllTypes.FromAssembly(typeof(Extensions).Assembly)
                    .BasedOn<IBusConfigurationAware>()
                );

            foreach (var configurationAware in container.ResolveAll<IBusConfigurationAware>())
            {
                configurationAware.Configure(configuration);
            }

            container.Kernel.ComponentModelCreated += Kernel_OnComponentModelCreated;
            container.Kernel.Resolver.AddSubResolver(new ArrayResolver(container.Kernel));

            foreach (var type in configuration.MessageModules)
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
                    .ImplementedBy(configuration.SerializerType),
                Component.For<IEndpointRouter>()
                    .ImplementedBy<EndpointRouter>()
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
    }
}