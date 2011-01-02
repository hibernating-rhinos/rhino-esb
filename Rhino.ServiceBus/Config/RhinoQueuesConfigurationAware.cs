using System;
using System.Configuration;
using System.IO;
using System.Transactions;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Config
{
    public abstract class RhinoQueuesConfigurationAware : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusFacility facility)
        {
            if (facility.Endpoint.Scheme.Equals("rhino.queues", StringComparison.InvariantCultureIgnoreCase) ==
                false)
                return;

            var busConfig = facility.ConfigurationSection.Bus;

            if (string.IsNullOrEmpty(busConfig.Name))
                throw new ConfigurationErrorsException(
                    "Could not find attribute 'name' in node 'bus' in configuration");

            var path = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

            RegisterTransportServices(facility.ThreadCount,
                                      facility.Endpoint,
                                      facility.IsolationLevel,
                                      facility.NumberOfRetries,
                                      path,
                                      busConfig.Name);
        }

        protected abstract void RegisterTransportServices(int threadCount, Uri endpoint,
                                                          IsolationLevel queueIsolationLevel, int numberOfRetries,
                                                          string path, string name);
    }
}