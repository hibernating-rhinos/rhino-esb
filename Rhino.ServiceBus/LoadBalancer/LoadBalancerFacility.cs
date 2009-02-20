using System;
using System.Configuration;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.LoadBalancer
{
    public class LoadBalancerFacility : AbstractRhinoServiceBusFacility
    {
        protected override void RegisterComponents()
        {
            Kernel.Register(
                Component.For<MsmqLoadBalancer>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .DependsOn(new
                    {
                        endpoint,
                        threadCount
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
        }
    }
}