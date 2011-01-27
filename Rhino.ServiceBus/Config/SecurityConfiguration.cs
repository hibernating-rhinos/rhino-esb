using System;
using System.Configuration;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Config
{
    public class SecurityConfiguration : IBusConfigurationAware 
    {
        public void Configure(AbstractRhinoServiceBusFacility config, IBusContainerBuilder builder)
        {
            var busConfig = config as RhinoServiceBusFacility;
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