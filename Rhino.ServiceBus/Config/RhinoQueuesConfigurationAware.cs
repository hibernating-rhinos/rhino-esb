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
            var busConfig = facility as RhinoServiceBusFacility;
            if (busConfig == null)
                return;

            if (facility.Endpoint.Scheme.Equals("rhino.queues", StringComparison.InvariantCultureIgnoreCase) ==
                false)
                return;

            var busConfigSection = facility.ConfigurationSection.Bus;

            if (string.IsNullOrEmpty(busConfigSection.Name))
                throw new ConfigurationErrorsException(
                    "Could not find attribute 'name' in node 'bus' in configuration");

            var path = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

            RegisterTransportServices(facility.ThreadCount,
                                      facility.Endpoint,
                                      facility.IsolationLevel,
                                      facility.NumberOfRetries,
                                      path,
                                      busConfigSection.Name);
        }

        protected abstract void RegisterTransportServices(int threadCount, Uri endpoint,
                                                          IsolationLevel queueIsolationLevel, int numberOfRetries,
                                                          string path, string name);
    }
}