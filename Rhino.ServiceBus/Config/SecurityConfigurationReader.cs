using System;
using System.Configuration;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Config
{
    public class SecurityConfigurationReader
    {
        public SecurityConfigurationReader(AbstractRhinoServiceBusFacility config)
        {
            if (config.ConfigurationSection.Security.Key == null)
                return;

            var key = config.ConfigurationSection.Security.Key;
            if (string.IsNullOrEmpty(key))
                throw new ConfigurationErrorsException("<security> element must have a <key> element with content");

            var keyBuffer = Convert.FromBase64String(key);

            Key = keyBuffer;
        }

        public byte[] Key { get; private set; }
    }
}