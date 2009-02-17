namespace Rhino.ServiceBus.DistributedHashTableIntegration
{
	using System.Linq;
	using Castle.Core;
	using Castle.MicroKernel;
	using Castle.MicroKernel.Facilities;
	using DistributedHashTable;
	using Exceptions;
	using Internal;
	using Messages;
	using Sagas;

	public class DhtClientSagaFacility : AbstractFacility
	{
		private void Kernel_OnComponentRegistered(string key, IHandler handler)
		{
			RegisterDhtSagaPersister(handler.ComponentModel);
		}

		private void RegisterDhtSagaPersister(ComponentModel model)
		{
			var list = model.Implementation.GetInterfaces()
				.Where(x => x.IsGenericType &&
							x.IsGenericTypeDefinition == false &&
							x.GetGenericTypeDefinition() == typeof(ISaga<>))
				.ToList();

			if (list.Count == 0)
				return;

			if (list.Count > 1)
			{
				throw new InvalidUsageException(model.Implementation +
												" implements more than one ISaga<T>, this is not permitted");
			}

			var sagaType = list[0];
			var sagaStateType = sagaType.GetGenericArguments()[0];

			if (typeof(Orchestrates<MergeSagaState>).IsAssignableFrom(model.Implementation))
			{
				Kernel.AddComponent(
					"SagaPersister<" + model.Implementation + ">",
					typeof(ISagaPersister<>)
						.MakeGenericType(model.Implementation),
					typeof(DistributedHashTableSagaPersister<,>)
						.MakeGenericType(model.Implementation, sagaStateType)
					);
			}
			else if (typeof(SupportsOptimisticConcurrency).IsAssignableFrom(model.Implementation))
			{
				Kernel.AddComponent(
					 "SagaPersister<" + model.Implementation + ">",
					 typeof(ISagaPersister<>)
						 .MakeGenericType(model.Implementation),
					 typeof(OptimisticDistributedHashTableSagaPersister<,>)
						 .MakeGenericType(model.Implementation, sagaStateType)
					 );
			}
			else
			{
				throw new InvalidUsageException(
					"When using DHT for saga state, you must specify either SupportsOptimisticConcurrency or Orchestrates<MergeSagaState>");
			}
		}

		protected override void Init()
		{
			Kernel.ComponentRegistered += Kernel_OnComponentRegistered;
			Kernel.AddComponent("dht", typeof(IDistributedHashTableClient), typeof(DistributedHashTableClient));
		}
	}
}