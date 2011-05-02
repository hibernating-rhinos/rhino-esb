using System;
using System.Configuration;
using log4net;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.LoadBalancer
{
    public class LoadBalancerConfiguration : AbstractRhinoServiceBusConfiguration
    {
        private readonly ILog logger = LogManager.GetLogger(typeof (LoadBalancerConfiguration));

        private Type loadBalancerType = typeof(MsmqLoadBalancer);
        private Uri secondaryLoadBalancer;
        private Uri primaryLoadBalancer;
        private Uri readyForWork;

        public Type LoadBalancerType
        {
            get { return loadBalancerType; }
        }

        public Uri PrimaryLoadBalancer
        {
            get { return primaryLoadBalancer; }
        }

        public Uri SecondaryLoadBalancer
        {
            get { return secondaryLoadBalancer; }
        }

        public Uri ReadyForWork
        {
            get { return readyForWork; }
        }

        protected override void ApplyConfiguration()
        {
            var busConfig = ConfigurationSection.LoadBalancer;
            if (busConfig == null)
                throw new ConfigurationErrorsException("Could not find 'loadBalancer' node in configuration");

            if(busConfig.ThreadCount.HasValue)
                ThreadCount = busConfig.ThreadCount.Value;

            string uriString = busConfig.Endpoint;
            Uri endpoint;
            if (Uri.TryCreate(uriString, UriKind.Absolute, out endpoint) == false)
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'endpoint' on 'loadBalancer' has an invalid value '" + uriString + "'");
            }
            Endpoint = endpoint;

            string readyForWorkEndPoint = busConfig.ReadForWorkEndpoint ?? uriString;

            if (Uri.TryCreate(readyForWorkEndPoint, UriKind.Absolute, out readyForWork) == false)
            {
                throw new ConfigurationErrorsException(
                "Attribute 'readyForWorkEndPoint' on 'loadBalancer' has an invalid value '" + readyForWorkEndPoint + "'");
            }

            var secondaryUri = busConfig.SecondaryLoadBalancerEndpoint;
            if (secondaryUri != null)//primary with secondary
            {
                if (Uri.TryCreate(secondaryUri, UriKind.Absolute, out secondaryLoadBalancer) == false)
                {
                    throw new ConfigurationErrorsException(
                        "Attribute 'secondaryLoadBalancerEndpoint' on 'loadBalancer' has an invalid value '" + secondaryUri + "'");
                }
            }
            var primaryUri = busConfig.PrimaryLoadBalancerEndpoint;
            if (primaryUri != null)//secondary with primary
            {
                loadBalancerType = typeof (MsmqSecondaryLoadBalancer);
                if(Uri.TryCreate(primaryUri, UriKind.Absolute, out primaryLoadBalancer)==false)
                {
                    throw new ConfigurationErrorsException(
                        "Attribute 'primaryLoadBalancerEndpoint' on 'loadBalancer' has an invalid value '" +
                        primaryLoadBalancer + "'");
                }
            }

            logger.InfoFormat("Configuring load balancer '{0}' with endpoint '{1}', primary '{2}', secondary '{3}'",
                loadBalancerType.Name,
                Endpoint,
                primaryLoadBalancer,
                secondaryLoadBalancer);
        }
    }
}