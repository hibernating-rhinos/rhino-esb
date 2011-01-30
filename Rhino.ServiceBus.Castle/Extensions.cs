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
    }
}