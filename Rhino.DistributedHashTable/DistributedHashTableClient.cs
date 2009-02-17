using System;
using System.Linq;

namespace Rhino.DistributedHashTable
{
	using PersistentHashTable;
	using Util;

	public class DistributedHashTableClient : IDistributedHashTableClient
	{
		public Node[] Nodes { get; private set; }

		public DistributedHashTableClient(Node metadata)
		{
			metadata.ExecuteSync(uri =>
			{
				ServiceUtil.Execute<IDistributedHashTableMetaDataProvider>(uri, provider =>
				{
					Nodes = provider.GetNetworkNodes();
				});
			});
		}

		public PutResult[] Put(params PutRequest[] valuesToAdd)
		{
			var groupedByUri = from x in valuesToAdd
			                   group x by GetUrl(x.Key);
			var versions = new PutResult[valuesToAdd.Length];
			foreach (var values in groupedByUri)
			{
				var array = values.ToArray();
				var versionForCurrentBatch = new PutResult[0];
				values.Key.ExecuteSync(uri =>
				{
					ServiceUtil.Execute<IDistributedHashTable>(uri, table =>
					{
						versionForCurrentBatch = table.Put(values.Key, array);
					});
				});
				for (int i = 0; i < array.Length; i++)
				{
					versions[Array.IndexOf(valuesToAdd, array[i])] = versionForCurrentBatch[i];
				}
			}
			return versions;
		}

		public Value[][] Get(params GetRequest[] valuesToGet)
		{
			var groupedByUri = from x in valuesToGet
			                   group x by GetUrl(x.Key);
			var valuesFromEndpoints = new Value[valuesToGet.Length][];
			foreach (var values in groupedByUri)
			{
				var array = values.ToArray();
				var valuesFromCurrentBatch = new Value[0][];
				values.Key.ExecuteSync(uri =>
				{
					ServiceUtil.Execute<IDistributedHashTable>(uri, table =>
					{
						valuesFromCurrentBatch = table.Get(array);
					});
				});
				for (var i = 0; i < array.Length; i++)
				{
					valuesFromEndpoints[Array.IndexOf(valuesToGet, array[i])] = valuesFromCurrentBatch[i];
				}
			}
			return valuesFromEndpoints;
		}

		public bool[] Remove(params RemoveRequest[] valuesToRemove)
		{
			var groupedByUri = from x in valuesToRemove
			                   group x by GetUrl(x.Key);
			var valuesFromEndpoints = new bool[valuesToRemove.Length];
			foreach (var values in groupedByUri)
			{
				var array = values.ToArray();
				var valuesFromCurrentBatch = new bool[0];

				values.Key.ExecuteSync(uri =>
				{
					ServiceUtil.Execute<IDistributedHashTable>(uri, table =>
					{
						valuesFromCurrentBatch = table.Remove(values.Key, array);
					});
				});

				for (int i = 0; i < array.Length; i++)
				{
					valuesFromEndpoints[Array.IndexOf(valuesToRemove, array[i])] = valuesFromCurrentBatch[i];
				}
			}
			return valuesFromEndpoints;
		}

		private Node GetUrl(string key)
		{
			var index = Math.Abs(key.GetHashCode()) % Nodes.Length;
			return Nodes[index];
		}
	}
}