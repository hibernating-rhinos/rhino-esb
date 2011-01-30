using System;
using System.Configuration;
using System.IO;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Config
{
    public class RhinoQueuesConfigurationAware : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusConfiguration configuration, IBusContainerBuilder builder)
        {
            var busConfig = configuration as RhinoServiceBusConfiguration;
            if (busConfig == null)
                return;

            if (configuration.Endpoint.Scheme.Equals("rhino.queues", StringComparison.InvariantCultureIgnoreCase) ==
                false)
                return;

            var busConfigSection = configuration.ConfigurationSection.Bus;

            if (string.IsNullOrEmpty(busConfigSection.Name))
                throw new ConfigurationErrorsException(
                    "Could not find attribute 'name' in node 'bus' in configuration");

            var path = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

            builder.RegisterRhinoQueuesTransport(path, busConfigSection.Name);
        }
    }
}