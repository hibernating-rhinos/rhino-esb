using System;
using System.Configuration;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.MessageModules;

namespace Rhino.ServiceBus.Config
{
    public class LoggingConfiguration : IBusConfigurationAware 
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder)
        {
            var busConfig = config as RhinoServiceBusConfiguration;
            if (busConfig == null)
                return;

            Uri logEndpoint;

            var uriString = config.ConfigurationSection.Bus.LogEndpoint;
            if (string.IsNullOrEmpty(uriString))
                return;

            if (Uri.TryCreate(uriString, UriKind.Absolute, out logEndpoint) == false)
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'logEndpoint' on 'bus' has an invalid value '" + uriString + "'");
            }
            builder.RegisterLoggingEndpoint(logEndpoint);
            config.InsertMessageModuleAtFirst<MessageLoggingModule>();
        }
    }
}