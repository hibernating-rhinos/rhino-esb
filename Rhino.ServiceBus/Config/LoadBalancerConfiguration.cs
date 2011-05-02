using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;

namespace Rhino.ServiceBus.Config
{
    public class LoadBalancerConfiguration : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder)
        {
            var loadBalancerConfig = config as LoadBalancer.LoadBalancerConfiguration;
            if (loadBalancerConfig == null)
                return;
            if (loadBalancerConfig.SecondaryLoadBalancer != null)
            {
                builder.RegisterSecondaryLoadBalancer();
            }
            else
            {
                builder.RegisterPrimaryLoadBalancer();
            }

            if (loadBalancerConfig.ReadyForWork != null)
            {
                builder.RegisterReadyForWork();
            }
        }
    }
}