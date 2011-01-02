using System;
using System.Configuration;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Config
{
    public class LoggingConfigurationReader
    {
        public LoggingConfigurationReader(AbstractRhinoServiceBusFacility configuration)
        {
            Uri logEndpoint;

            var uriString = configuration.ConfigurationSection.Bus.LogEndpoint;
            if (string.IsNullOrEmpty(uriString))
                return;

            if (Uri.TryCreate(uriString, UriKind.Absolute, out logEndpoint) == false)
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'logEndpoint' on 'bus' has an invalid value '" + uriString + "'");
            }
            LogEndpoint = logEndpoint;
        }

        public Uri LogEndpoint { get; private set; }
    }
}