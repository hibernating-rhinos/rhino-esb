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

        public void Configure(AbstractRhinoServiceBusFacility facility, IBusContainerBuilder builder)
        {
            if (!(facility is RhinoServiceBusFacility) && !(facility is LoadBalancerFacility))
                return;

            if (facility.Endpoint.Scheme.Equals("msmq", StringComparison.InvariantCultureIgnoreCase) == false)
                return;

            if (facility.UseFlatQueue)
            {
                queueStrategyImpl = typeof (FlatQueueStrategy);
            }

            if (facility.DisableAutoQueueCreation == false)
            {
                builder.RegisterQueueCreation();
            }

            builder.RegisterMsmqTransport(queueStrategyImpl);
        }
    }
}