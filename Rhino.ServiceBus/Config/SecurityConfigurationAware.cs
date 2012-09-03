using System;
using System.Configuration;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Config
{
    public class SecurityConfigurationAware : IBusConfigurationAware 
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder, IServiceLocator locator)
        {
            var busConfig = config as RhinoServiceBusConfiguration;
            if (busConfig == null)
                return;
            if (config.ConfigurationSection.Security.Key == null)
            {
                builder.RegisterNoSecurity();
                return;
            }

            var key = config.ConfigurationSection.Security.Key;
            if (string.IsNullOrEmpty(key))
                throw new ConfigurationErrorsException("<security> element must have a <key> element with content");

            var keyBuffer = Convert.FromBase64String(key);

            builder.RegisterSecurity(keyBuffer);
        }
    }
}