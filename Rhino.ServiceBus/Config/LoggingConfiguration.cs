using System;
using System.Configuration;
using Castle.Core.Configuration;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.MessageModules;

namespace Rhino.ServiceBus.Config
{
    public class LoggingConfiguration : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusFacility facility, IConfiguration configuration)
        {
            var kernel = facility.Kernel;
            Uri logEndpoint;

            var bus = configuration.Children["bus"];
            if(bus==null)
                return;
            
            var uriString = bus.Attributes["logEndpoint"];
            if (uriString == null)
                return;

            if (Uri.TryCreate(uriString, UriKind.Absolute, out logEndpoint) == false)
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'logEndpoint' on 'bus' has an invalid value '" + uriString + "'");
            }

            kernel.Register(
                Component.For<MessageLoggingModule>()
                    .DependsOn(new {logQueue = logEndpoint})
                );

            facility.InsertMessageModuleAtFirst<MessageLoggingModule>();
        }
    }
}