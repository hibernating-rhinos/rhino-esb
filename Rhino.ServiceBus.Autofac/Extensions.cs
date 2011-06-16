using Autofac;
using Rhino.ServiceBus.Autofac;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus
{
    public static class Extensions
    {
        public static AbstractRhinoServiceBusConfiguration UseAutofac(this AbstractRhinoServiceBusConfiguration configuration)
        {
            return UseAutofac(configuration, new ContainerBuilder().Build());
        }

        public static AbstractRhinoServiceBusConfiguration UseAutofac(this AbstractRhinoServiceBusConfiguration configuration, IContainer container)
        {
            new AutofacBuilder(configuration, container);
            return configuration;
        }
    }
}