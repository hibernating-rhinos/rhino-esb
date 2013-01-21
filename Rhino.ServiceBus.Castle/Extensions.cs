using System;
using System.Reflection;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
 
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
            RegisterConsumersFrom(container, assembly, x=>x.Named(x.Implementation.FullName), x=>true);
        }
 
        public static void RegisterConsumersFrom(this IWindsorContainer container, Assembly assembly, Action<ComponentRegistration> configureConsumer, Func<Type, bool> isTypeAcceptable)
        {
            container.Register(
                 AllTypes
                    .FromAssembly(assembly)
                    .Where(type =>
                        typeof(IMessageConsumer).IsAssignableFrom(type) &&
                        !typeof(IOccasionalMessageConsumer).IsAssignableFrom(type) &&
                        isTypeAcceptable(type)
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