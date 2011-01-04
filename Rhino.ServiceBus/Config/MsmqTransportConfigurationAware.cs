using System;
using System.Transactions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Config
{
    public abstract class MsmqTransportConfigurationAware : IBusConfigurationAware
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

        protected Type QueueStrategyType
        {
            get { return queueStrategyImpl; }
        }

        protected MsmqTransportConfigurationAware()
        {
            DetectQueueStrategy();
        }

        public void Configure(AbstractRhinoServiceBusFacility facility)
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
                RegisterQueueCreationModule();
            }

            RegisterTransportServices(facility.ThreadCount,
                                      facility.Endpoint,
                                      facility.IsolationLevel,
                                      facility.NumberOfRetries,
                                      facility.Transactional,
                                      facility.consumeInTxn);
        }

        protected abstract void RegisterQueueCreationModule();

        protected abstract void RegisterTransportServices(int threadCount, 
            Uri endpoint, 
            IsolationLevel queueIsolationLevel, 
            int numberOfRetries, 
            TransactionalOptions transactionalOptions, 
            bool consumeInTransaction);
    }
}