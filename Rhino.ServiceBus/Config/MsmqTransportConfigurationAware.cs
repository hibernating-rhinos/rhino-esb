using System;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Config
{
    public class MsmqTransportConfigurationAware : IBusConfigurationAware
    {
        private Type queueStrategyImpl = typeof(SubQueueStrategy);

        /// <summary>
        /// Detects the valid queue strategy automatically.
        /// </summary>
        private void DetectQueueStrategy()
        {
            if (Environment.OSVersion.Version.Major <= 5)
            {
                queueStrategyImpl = typeof(FlatQueueStrategy);
            }
        }

        public MsmqTransportConfigurationAware()
        {
            DetectQueueStrategy();
        }

        public void Configure(AbstractRhinoServiceBusConfiguration configuration, IBusContainerBuilder builder)
        {
            if (!(configuration is RhinoServiceBusConfiguration) && !(configuration is LoadBalancer.LoadBalancerConfiguration))
                return;

            if (configuration.Endpoint.Scheme.Equals("msmq", StringComparison.InvariantCultureIgnoreCase) == false)
                return;

            if (configuration.UseFlatQueue)
            {
                queueStrategyImpl = typeof (FlatQueueStrategy);
            }

            if (configuration.DisableAutoQueueCreation == false)
            {
                builder.RegisterQueueCreation();
            }

            builder.RegisterMsmqTransport(queueStrategyImpl);
        }
    }
}