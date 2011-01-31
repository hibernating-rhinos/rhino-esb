using System;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;

namespace Rhino.ServiceBus.StructureMap
{
    [CLSCompliant(false)]
    public class StructureMapLoadBalancerBootStrapper : StructureMapBootStrapper
    {
        protected override AbstractRhinoServiceBusConfiguration CreateConfiguration()
        {
            return new LoadBalancerConfiguration();
        }
    }
}