using System;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Msmq.TransportActions;

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

        public void Configure(AbstractRhinoServiceBusFacility facility, IConfiguration configuration)
        {
            if (facility.Endpoint.Scheme.Equals("msmq", StringComparison.InvariantCultureIgnoreCase) == false)
                return;

            if(facility.UseFlatQueue)
            {
                queueStrategyImpl = typeof (FlatQueueStrategy);
            }

            if(facility.DisableAutoQueueCreation==false)
            {
                facility.Kernel.Register(Component.For<QueueCreationModule>());
            }

            facility.Kernel.Register(
                Component.For<IQueueStrategy>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(queueStrategyImpl)
                    .DependsOn(new { endpoint = facility.Endpoint }),
                Component.For<IMsmqTransportAction>()
                    .ImplementedBy<ErrorAction>()
                    .DependsOn(new { numberOfRetries = facility.NumberOfRetries }),
                Component.For<ISubscriptionStorage>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(MsmqSubscriptionStorage))
                    .DependsOn(new
                    {
                        queueBusListensTo = facility.Endpoint
                    }),
                Component.For<ITransport>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(MsmqTransport))
                    .DependsOn(new
                    {
                        threadCount = facility.ThreadCount,
                        endpoint = facility.Endpoint,
                        queueIsolationLevel = facility.IsolationLevel,
                        numberOfRetries = facility.NumberOfRetries
                    }),
                AllTypes.Of<IMsmqTransportAction>()
                    .FromAssembly(typeof(IMsmqTransportAction).Assembly)
                    .Unless(x => x == typeof(ErrorAction))
                    .WithService.FirstInterface()
                    .Configure(registration =>
                               registration.LifeStyle.Is(LifestyleType.Singleton))
                );
        }
    }
}