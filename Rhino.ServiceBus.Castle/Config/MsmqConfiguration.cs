using System;
using System.Transactions;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Msmq.TransportActions;

namespace Rhino.ServiceBus.Castle.Config
{
    public class MsmqConfiguration : MsmqTransportConfigurationAware
    {
        private readonly IWindsorContainer container;

        public MsmqConfiguration(IWindsorContainer container)
        {
            this.container = container;
        }

        protected override void RegisterQueueCreationModule()
        {
            container.Kernel.Register(Component.For<QueueCreationModule>());
        }

        protected override void RegisterTransportServices(int threadCount, Uri endpoint, IsolationLevel queueIsolationLevel, int numberOfRetries, TransactionalOptions transactionalOptions, bool consumeInTransaction)
        {
            container.Kernel.Register(
                Component.For<IQueueStrategy>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(QueueStrategyType)
                    .DependsOn(new { endpoint }),
                Component.For<IMsmqTransportAction>()
                    .ImplementedBy<ErrorAction>()
                    .DependsOn(new { numberOfRetries }),
                Component.For<ISubscriptionStorage>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(MsmqSubscriptionStorage))
                    .DependsOn(new
                    {
                        queueBusListensTo = endpoint
                    }),
                Component.For<ITransport>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .ImplementedBy(typeof(MsmqTransport))
                    .DependsOn(new
                    {
                        threadCount,
                        endpoint,
                        queueIsolationLevel,
                        numberOfRetries,
                        transactional = transactionalOptions,
                        consumeInTransaction,
                    }),
                AllTypes.FromAssembly(typeof(IMsmqTransportAction).Assembly)
                    .BasedOn<IMsmqTransportAction>()
                    .Unless(x => x == typeof(ErrorAction))
                    .WithService.FirstInterface()
                    .Configure(registration =>
                               registration.LifeStyle.Is(LifestyleType.Singleton))
                );
        }
    }
}