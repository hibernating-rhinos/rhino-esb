using System;
using System.Configuration;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using log4net;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.LoadBalancer
{
    public class LoadBalancerFacility : AbstractRhinoServiceBusFacility
    {
        private readonly ILog logger = LogManager.GetLogger(typeof (LoadBalancerFacility));

        private Type loadBalancerType = typeof(MsmqLoadBalancer);
        private Uri secondaryLoadBalancer;
        private Uri primaryLoadBalancer;

        protected override void RegisterComponents()
        {
            logger.InfoFormat("Configuring load balancer '{0}' with endpoint '{1}', primary '{2}', secondary '{3}'",
                loadBalancerType.Name,
                endpoint,
                primaryLoadBalancer,
                secondaryLoadBalancer);

            Kernel.Register(
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateLoadBalancerQueuesAction>(),
                Component.For<MsmqLoadBalancer>()
                    .ImplementedBy(loadBalancerType)
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .DependsOn(new
                    {
                        endpoint,
                        threadCount,
                        secondaryLoadBalancer,
                        primaryLoadBalancer
                    })
                );
        }

        protected override void ReadConfiguration()
        {
            IConfiguration busConfig = FacilityConfig.Children["loadBalancer"];
            if (busConfig == null)
                throw new ConfigurationErrorsException("Could not find 'loadBalancer' node in confiuration");

            int result;
            string threads = busConfig.Attributes["threadCounts"];
            if (int.TryParse(threads, out result))
                threadCount = result;

            string uriString = busConfig.Attributes["endpoint"];
            if (Uri.TryCreate(uriString, UriKind.Absolute, out endpoint) == false)
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'endpoint' on 'loadBalancer' has an invalid value '" + uriString + "'");
            }

            var secondaryUri = busConfig.Attributes["secondaryLoadBalancerEndpoint"];
            if (secondaryUri != null)//primary with secondary
            {
                if (Uri.TryCreate(secondaryUri, UriKind.Absolute, out secondaryLoadBalancer) == false)
                {
                    throw new ConfigurationErrorsException(
                        "Attribute 'secondaryLoadBalancerEndpoint' on 'loadBalancer' has an invalid value '" + secondaryUri + "'");
                }
            }
            var primaryUri = busConfig.Attributes["primaryLoadBalancerEndpoint"];
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
        }
    }
}