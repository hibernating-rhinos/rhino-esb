using System;
using Rhino.ServiceBus.LoadBalancer;

namespace Rhino.ServiceBus.Spring
{
    [CLSCompliant(false)]
    public sealed class SpringLoadBalancerBootStrapper : SpringBootStrapper
    {
        protected override Impl.AbstractRhinoServiceBusConfiguration CreateConfiguration()
        {
            return new LoadBalancerConfiguration();
        }
    }
}