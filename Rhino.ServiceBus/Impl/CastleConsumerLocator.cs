using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Proxy;
using Rhino.Queues.Utils;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus.Impl
{
    public class CastleConsumerLocator : IConsumerLocator
    {
        private readonly IReflection reflection;
        private readonly IKernel kernel;
        private readonly ISubscriptionStorage subscriptionStorage;

        public CastleConsumerLocator(IReflection reflection, IKernel kernel, ISubscriptionStorage subscriptionStorage)
        {
            this.reflection = reflection;
            this.kernel = kernel;
            this.subscriptionStorage = subscriptionStorage;
        }

        public object[] GatherConsumers(object message)
        {
            object[] sagas = GetSagasFor(message);
            var sagaMessage = message as ISagaMessage;

            var msgType = message.GetType();
            object[] instanceConsumers = subscriptionStorage
                .GetInstanceSubscriptions(msgType);

            var consumerTypes = reflection.GetGenericTypesOfWithBaseTypes(typeof(ConsumerOf<>), message);
            var occasionalConsumerTypes = reflection.GetGenericTypesOfWithBaseTypes(typeof(OccasionalConsumerOf<>), message);
            var consumers = GetAllNonOccasionalConsumers(consumerTypes, occasionalConsumerTypes, sagas);
            for (var i = 0; i < consumers.Length; i++)
            {
                var saga = consumers[i] as IAccessibleSaga;
                if (saga == null)
                    continue;

                // if there is an existing saga, we skip the new one
                var type = saga.GetType();
                if (sagas.Any(type.IsInstanceOfType))
                {
                    kernel.ReleaseComponent(consumers[i]);
                    consumers[i] = null;
                    continue;
                }
                // we do not create new sagas if the saga is not initiated by
                // the message
                var initiatedBy = reflection.GetGenericTypeOf(typeof(InitiatedBy<>),msgType);
                if(initiatedBy.IsInstanceOfType(saga)==false)
                {
                    kernel.ReleaseComponent(consumers[i]);
                    consumers[i] = null;
                    continue;
                }

                saga.Id = sagaMessage != null ?
                    sagaMessage.CorrelationId :
                    GuidCombGenerator.Generate();
            }
            return instanceConsumers
                .Union(sagas)
                .Union(consumers.Where(x => x != null))
                .ToArray();
        }


        /// <summary>
        /// Here we don't use ResolveAll from Windsor because we want to get an error
        /// if a component exists which isn't valid
        /// </summary>
        private object[] GetAllNonOccasionalConsumers(IEnumerable<Type> consumerTypes, IEnumerable<Type> occasionalConsumerTypes, IEnumerable<object> instanceOfTypesToSkipResolving)
        {
            var allHandlers = new List<IHandler>();
            foreach (var consumerType in consumerTypes)
            {
                var handlers = kernel.GetAssignableHandlers(consumerType);
                allHandlers.AddRange(handlers);
            }

            var consumers = new List<object>(allHandlers.Count);
            consumers.AddRange(from handler in allHandlers
                               let implementation = handler.ComponentModel.Implementation
                               let occasionalConsumerFound = occasionalConsumerTypes.Any(occasionalConsumerType => occasionalConsumerType.IsAssignableFrom(implementation))
                               where !occasionalConsumerFound
                               where !instanceOfTypesToSkipResolving.Any(x => x.GetType() == implementation)
                               select handler.Resolve(CreationContext.Empty));
            return consumers.ToArray();
        }

        private object[] GetSagasFor(object message)
        {
            var instances = new List<object>();

            Type orchestratesType = reflection.GetGenericTypeOf(typeof(Orchestrates<>), message);
            Type initiatedByType = reflection.GetGenericTypeOf(typeof(InitiatedBy<>), message);

            var handlers = kernel.GetAssignableHandlers(orchestratesType)
                                                   .Union(kernel.GetAssignableHandlers(initiatedByType));

            foreach (IHandler sagaHandler in handlers)
            {
                Type sagaType = sagaHandler.ComponentModel.Implementation;
                
                //first try to execute any saga finders.
                Type sagaFinderType = reflection.GetGenericTypeOf(typeof (ISagaFinder<,>), sagaType, ProxyUtil.GetUnproxiedType(message));
                var sagaFinderHandlers = kernel.GetAssignableHandlers(sagaFinderType);
                foreach (var sagaFinderHandler in sagaFinderHandlers)
                {
                    try
                    {
                        var sagaFinder = kernel.Resolve(sagaFinderHandler.Service);
                        var saga = reflection.InvokeSagaFinderFindBy(sagaFinder, message);
                        if (saga != null)
                            instances.Add(saga);
                    }
                    finally
                    {
                        kernel.ReleaseComponent(sagaFinderHandler);
                    }
                }

                //we will try to use an ISagaMessage's Correlation id next.
                var sagaMessage = message as ISagaMessage;
                if (sagaMessage == null)
                    continue;

                Type sagaPersisterType = reflection.GetGenericTypeOf(typeof(ISagaPersister<>),
                                                                     sagaType);

                object sagaPersister = kernel.Resolve(sagaPersisterType);
                try
                {
                    object sagas = reflection.InvokeSagaPersisterGet(sagaPersister, sagaMessage.CorrelationId);
                    if (sagas == null)
                        continue;
                    instances.Add(sagas);
                }
                finally
                {
                    kernel.ReleaseComponent(sagaPersister);
                }
            }
            return instances.ToArray();
        }
    }
}