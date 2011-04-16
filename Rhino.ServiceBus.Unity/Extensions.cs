using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Unity
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
