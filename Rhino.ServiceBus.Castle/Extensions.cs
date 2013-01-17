using Castle.Windsor;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus
{
    public static class Extensions
    {
        public static AbstractRhinoServiceBusConfiguration UseCastleWindsor(this AbstractRhinoServiceBusConfiguration configuration)
        {
            return UseCastleWindsor(configuration, new WindsorContainer());
        }

        public static AbstractRhinoServiceBusConfiguration UseCastleWindsor(this AbstractRhinoServiceBusConfiguration configuration, IWindsorContainer container)
        {
            new CastleBuilder(container, configuration);
            return configuration;
        }

        public static void RegisterConsumersFrom(this IWindsorContainer container, Assembly assembly)
        {
            RegisterConsumersFrom(container, assembly, x=>x.Named(x.Implementation.FullName));
        }

        public static void RegisterConsumersFrom(this IWindsorContainer container, Assembly assembly, Action<ComponentRegistration> configureConsumer)
        {
            container.Register(
                 AllTypes
                    .FromAssembly(assembly)
                    .Where(type =>
                        typeof(IMessageConsumer).IsAssignableFrom(type) &&
                        !typeof(IOccasionalMessageConsumer).IsAssignableFrom(type) &&
                        IsTypeAcceptableForThisBootStrapper(type)
                    )
                    .Configure(registration =>
                    {
                        registration.LifeStyle.Is(LifestyleType.Transient);
                        configureConsumer(registration);
                    })
                );
        }
    }
}