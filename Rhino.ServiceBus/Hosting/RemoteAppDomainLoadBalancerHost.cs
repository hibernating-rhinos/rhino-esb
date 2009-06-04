using System;
using System.Reflection;
using Rhino.ServiceBus.LoadBalancer;

namespace Rhino.ServiceBus.Hosting
{
    public class RemoteAppDomainLoadBalancerHost : RemoteAppDomainHost
    {
        public RemoteAppDomainLoadBalancerHost(Type boosterType) : base(boosterType)
        {
        }

        public RemoteAppDomainLoadBalancerHost(string assemblyPath, string configuration)
            : base(assemblyPath, configuration)
        {
        }

        public RemoteAppDomainLoadBalancerHost(Assembly assembly, string configuration) : 
            base(assembly, configuration)
        {
        }

        protected override HostedService CreateRemoteHost(AppDomain appDomain)
        {
            object instance = appDomain.CreateInstanceAndUnwrap("Rhino.ServiceBus",
                                                                "Rhino.ServiceBus.LoadBalancer.LoadBalancerHost");
            var hoster = (LoadBalancerHost)instance;
            return new HostedService(hoster, "Rhino.ServiceBus", appDomain);
        }
    }
}