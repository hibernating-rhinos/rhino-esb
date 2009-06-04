using System;
using System.Configuration;
using System.IO;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.RhinoQueues;

namespace Rhino.ServiceBus.Config
{
    public class RhinoQueuesConfigurationAware : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusFacility facility, IConfiguration configuration)
        {
            if (facility.Endpoint.Scheme.Equals("rhino.queues", StringComparison.InvariantCultureIgnoreCase) == false)
                return;

            IConfiguration busConfig = facility.FacilityConfig.Children["bus"];
            if (busConfig == null)
                throw new ConfigurationErrorsException("Could not find 'bus' node in configuration");
            var name = busConfig.Attributes["name"];
            if (string.IsNullOrEmpty(name))
                throw new ConfigurationErrorsException("Could not find attribute 'name' in node 'bus' in configuration");

            var path = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

            facility.Kernel.Register(
                Component.For<ISubscriptionStorage>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(PhtSubscriptionStorage))
                    .DependsOn(new
                    {
                        subscriptionPath = Path.Combine(path, name + "_subscriptions.esent")
                    }),
                Component.For<ITransport>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(RhinoQueuesTransport))
                    .DependsOn(new
                    {
                        threadCount = facility.ThreadCount,
                        endpoint = facility.Endpoint,
                        queueIsolationLevel = facility.IsolationLevel,
                        numberOfRetries = facility.NumberOfRetries,
                        path = Path.Combine(path, name + ".esent")
                    })
                );
        }
    }
}