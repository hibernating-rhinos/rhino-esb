namespace Rhino.DistributedHashTable
{
	using System;
	using System.Collections.Generic;
	using PersistentHashTable;

	public class LocalDistributedHashTable : IDistributedHashTableClient, IDisposable
	{
		private readonly PersistentHashTable persistentHashTable;

		public LocalDistributedHashTable(string database)
		{
			persistentHashTable = new PersistentHashTable(database);
			persistentHashTable.Initialize();
		}

		public PutResult[] Put(params PutRequest[] valuesToAdd)
		{
			var puts = new List<PutResult>();
			persistentHashTable.Batch(actions =>
			{
				foreach (var request in valuesToAdd)
				{
					var put = actions.Put(request);
					puts.Add(put);
				}

				actions.Commit();
			});
			return puts.ToArray();
		}

		public Value[][] Get(params GetRequest[] valuesToGet)
		{
			var valuesOfValues = new List<Value[]>();
			persistentHashTable.Batch(actions =>
			{
				foreach (var request in valuesToGet)
				{
					var values = actions.Get(request);
					valuesOfValues.Add(values);
				}
				actions.Commit();
			});
			return valuesOfValues.ToArray();
		}

		public bool[] Remove(params RemoveRequest[] valuesToRemove)
		{
			var removedValues = new List<bool>();
			persistentHashTable.Batch(actions =>
			{
				foreach (var request in valuesToRemove)
				{
					var removed = actions.Remove(request);
					removedValues.Add(removed);
				}
				actions.Commit();
			});
			return removedValues.ToArray();
		}

		public void Dispose()
		{
			persistentHashTable.Dispose();
		}
	}
}