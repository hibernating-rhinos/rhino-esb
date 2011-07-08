using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Spring;

using Spring.Context;
using Spring.Context.Support;

namespace Rhino.ServiceBus
{
    public static class Extensions
    {
        public static AbstractRhinoServiceBusConfiguration UseSpring(this AbstractRhinoServiceBusConfiguration configuration)
        {
            return UseSpring(configuration, new StaticApplicationContext());
        }

        public static AbstractRhinoServiceBusConfiguration UseSpring(this AbstractRhinoServiceBusConfiguration configuration, IConfigurableApplicationContext container)
        {
            new SpringBuilder(configuration, container);
            return configuration;
        }
    }
}