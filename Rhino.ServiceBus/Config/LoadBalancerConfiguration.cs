using System;
using System.Configuration;
using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.MessageModules;

namespace Rhino.ServiceBus.Config
{
    public class LoadBalancerConfiguration : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusFacility facility, IConfiguration configuration)
        {
            var bus = configuration.Children["bus"];
            if (bus == null)
                return;
            
            var loadBalancerEndpointAsString = bus.Attributes["loadBalancerEndpoint"];

            if(string.IsNullOrEmpty(loadBalancerEndpointAsString))
                return;

            Uri loadBalancerEndpoint;
            if (Uri.TryCreate(
                loadBalancerEndpointAsString, 
                UriKind.Absolute, 
                out loadBalancerEndpoint) == false)
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'loadBalancerEndpoint' on 'bus' has an invalid value '" + loadBalancerEndpointAsString + "'");
            }
            var endpoint = new Endpoint {Uri = loadBalancerEndpoint};
            facility.Kernel.Register(
                Component.For<LoadBalancerMessageModule>()
                    .DependsOn(new { loadBalancerEndpoint = endpoint.Uri })
                );

            facility.AddMessageModule<LoadBalancerMessageModule>();
        }
    }
}