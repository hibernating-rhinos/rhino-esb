using Castle.Windsor;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Impl;

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
            new CastleBuilder(container, configuration);
            return configuration;
        }
    }
}