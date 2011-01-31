using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;

namespace Rhino.ServiceBus.Castle
{
    public sealed class CastleLoadBalancerBootStrapper : CastleBootStrapper
    {
        protected override AbstractRhinoServiceBusConfiguration CreateConfiguration()
        {
            return new LoadBalancerConfiguration();
        }
    }
}