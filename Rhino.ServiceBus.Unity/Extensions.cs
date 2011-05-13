using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Unity;

namespace Rhino.ServiceBus
{
    public static class Extensions
    {
        public static AbstractRhinoServiceBusConfiguration UseUnity(this AbstractRhinoServiceBusConfiguration configuration)
        {
            return UseUnity(configuration, new UnityContainer());
        }

        public static AbstractRhinoServiceBusConfiguration UseUnity(this AbstractRhinoServiceBusConfiguration configuration, IUnityContainer container)
        {
            new UnityBuilder(container, configuration);
            return configuration;
        }
    }
}
