using System;
using System.IO;
using System.Linq;
using Castle.MicroKernel;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.DistributedHashTableIntegration
{
	using DistributedHashTable;
	using PersistentHashTable;
	using Sagas;

	public class OptimisticDistributedHashTableSagaPersister<TSaga, TState> : ISagaPersister<TSaga>
		where TSaga : class, ISaga<TState>
		where TState : IVersionedSagaState
	{
		private readonly IDistributedHashTableClient distributedHashTable;
		private readonly IReflection reflection;
		private readonly IMessageSerializer messageSerializer;
		private readonly IKernel kernel;

		public OptimisticDistributedHashTableSagaPersister(IDistributedHashTableClient distributedHashTable, IReflection reflection, IMessageSerializer messageSerializer, IKernel kernel)
		{
			this.distributedHashTable = distributedHashTable;
			this.reflection = reflection;
			this.messageSerializer = messageSerializer;
			this.kernel = kernel;
		}

		private static string CreateKey(Guid id)
		{
			return typeof(TSaga).FullName + "-" + id;
		}

		public TSaga Get(Guid id)
		{
			var values = distributedHashTable.Get(new[]
			{
				new GetRequest {Key = CreateKey(id)},
			}).First();

			if(values.Length==0)
				return null;

			var value = values[0];

			TState state;
			using (var ms = new MemoryStream(value.Data))
			{
				var msgs = messageSerializer.Deserialize(ms);
				state = (TState)msgs[0];
				state.Version = value.Version;
				state.ParentVersions = value.ParentVersions;
			}
			var saga = kernel.Resolve<TSaga>();
			saga.Id = id;
			reflection.Set(saga, "State", type => state);
			return saga;
		}

		public void Save(TSaga saga)
		{
			using (var message = new MemoryStream())
			{
				var state = (TState)reflection.Get(saga, "State");
				messageSerializer.Serialize(new object[] { state }, message);
				var putResults = distributedHashTable.Put(new[]
				{
					new PutRequest
					{
						Bytes = message.ToArray(),
						Key = CreateKey(saga.Id),
						OptimisticConcurrency = true,
						ParentVersions = (state.Version != null ? new[] { state.Version } : new ValueVersion[0])
					},
				});
				if (putResults[0].ConflictExists)
				{
					throw new OptimisticConcurrencyException("Saga state is not the latest: " + saga.Id);
				}
			}
		}

		public void Complete(TSaga saga)
		{
			var state = (TState) reflection.Get(saga, "State");
			var removed = distributedHashTable.Remove(new[]
			{
				new RemoveRequest
				{
					Key = CreateKey(saga.Id),
					ParentVersions = new []{state.Version}
				},
			});
			if (removed[0] == false)
			{
				throw new OptimisticConcurrencyException("Saga state is not the latest: " + saga.Id);
			}
		}
	}
}