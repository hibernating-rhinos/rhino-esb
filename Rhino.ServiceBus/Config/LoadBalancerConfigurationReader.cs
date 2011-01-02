using System;
using System.Configuration;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Config
{
    public class LoadBalancerConfigurationReader
    {
        public LoadBalancerConfigurationReader(AbstractRhinoServiceBusFacility configuration)
        {
            var busElement = configuration.ConfigurationSection.Bus;

            if (busElement == null)
                return;

            var loadBalancerEndpointAsString = busElement.LoadBalancerEndpoint;

            if (string.IsNullOrEmpty(loadBalancerEndpointAsString))
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
            LoadBalancerEndpoint = loadBalancerEndpoint;
        }

        public Uri LoadBalancerEndpoint { get; private set; }
    }
}