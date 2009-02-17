using System;
using System.IO;
using Rhino.ServiceBus.Internal;
using System.Linq;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus.DistributedHashTableIntegration
{
	using Castle.MicroKernel;
	using DistributedHashTable;
	using PersistentHashTable;
	using Sagas;

	public class DistributedHashTableSagaPersister<TSaga, TState> : ISagaPersister<TSaga>
		where TSaga : class, ISaga<TState>
		where TState : IVersionedSagaState
	{
		private readonly IDistributedHashTableClient distributedHashTable;
		private readonly ISagaStateMerger<TState> stateMerger;
		private readonly IMessageSerializer messageSerializer;
		private readonly IKernel kernel;
		private readonly IServiceBus bus;
		private readonly IReflection reflection;

		public DistributedHashTableSagaPersister(IDistributedHashTableClient distributedHashTable, ISagaStateMerger<TState> stateMerger, IMessageSerializer messageSerializer, IKernel kernel, IReflection reflection, IServiceBus bus)
		{
			this.distributedHashTable = distributedHashTable;
			this.bus = bus;
			this.stateMerger = stateMerger;
			this.messageSerializer = messageSerializer;
			this.kernel = kernel;
			this.reflection = reflection;
		}


		private static string CreateKey(Guid id)
		{
			return typeof(TSaga).FullName + "-" + id;
		}


		public TSaga Get(Guid id)
		{
			var values = distributedHashTable.Get(new[]
			{
				new GetRequest{Key = CreateKey(id)},
			}).First();
			if (values.Length == 0)
				return null;
			TState state;
			if (values.Length != 1)
			{
				var states = new TState[values.Length];
				for (var i = 0; i < values.Length; i++)
				{
					var value = values[i];
					using (var ms = new MemoryStream(value.Data))
					{
						object[] msgs = messageSerializer.Deserialize(ms);
						states[i] = (TState)msgs[0];
						states[i].ParentVersions = value.ParentVersions;
						states[i].Version = value.Version;
					}
				}
				state = stateMerger.Merge(states);
				state.ParentVersions = values
					.Select(x => x.Version)
					.ToArray();
			}
			else
			{
				using (var ms = new MemoryStream(values[0].Data))
				{
					object[] msgs = messageSerializer.Deserialize(ms);
					state = (TState)msgs[0];
					state.ParentVersions = new[] { values[0].Version };
				}
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
						ParentVersions = state.ParentVersions ?? new ValueVersion[0]
					},
				});
				if(putResults[0].ConflictExists)
				{
					bus.Send(bus.Endpoint, new MergeSagaState
					{
						CorrelationId = saga.Id
					});
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
					ParentVersions = state.ParentVersions ?? new ValueVersion[0]
				},
			});
			if (removed[0] == false)
			{
				bus.Send(bus.Endpoint, new MergeSagaState
				{
					CorrelationId = saga.Id
				});
			}
		}
	}
}