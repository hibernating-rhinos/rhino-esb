using System;
using System.Configuration;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;

namespace Rhino.ServiceBus.Config
{
    public class LoggingConfigurationAware : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder, IServiceLocator locator)
        {
            var busConfig = config as RhinoServiceBusConfiguration;
            if (busConfig == null)
                return;

            var uriString = config.ConfigurationSection.Bus.LogEndpoint;
            if (string.IsNullOrEmpty(uriString))
                return;

            Uri logEndpoint;
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out logEndpoint))
                throw new ConfigurationErrorsException(
                    "Attribute 'logEndpoint' on 'bus' has an invalid value '" + uriString + "'");

            builder.RegisterLoggingEndpoint(logEndpoint);
            busConfig.InsertMessageModuleAtFirst<MessageLoggingModule>();
        }
    }
}