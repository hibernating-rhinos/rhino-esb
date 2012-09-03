using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Config
{
    public class LoadBalancerConfigurationAware : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder, IServiceLocator locator)
        {
            var loadBalancerConfig = config as LoadBalancer.LoadBalancerConfiguration;
            if (loadBalancerConfig == null)
                return;

            if (loadBalancerConfig.SecondaryLoadBalancer != null)
                builder.RegisterSecondaryLoadBalancer();
            else
                builder.RegisterPrimaryLoadBalancer();

            if (loadBalancerConfig.ReadyForWork != null)
                builder.RegisterReadyForWork();
        }
    }
}