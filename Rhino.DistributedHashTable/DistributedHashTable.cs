namespace Rhino.DistributedHashTable
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;
	using System.Transactions;
	using Messages;
	using PersistentHashTable;
	using ServiceBus.Internal;
	using ServiceBus;
	using Util;

	[ServiceBehavior(
		ConcurrencyMode = ConcurrencyMode.Multiple,
		InstanceContextMode = InstanceContextMode.Single,
		IncludeExceptionDetailInFaults = true
		)]
	public class DistributedHashTable : IDistributedHashTable
	{
		private readonly IEndpointRouter endpointRouter;

		private readonly IServiceBus bus;

		const string rhinoDhtStartToken = "@rdht://";

		public Node Metadata { get; set; }

		private readonly PersistentHashTable hashTable;

		private Node failOver;

		public DistributedHashTable(
			string database,
			Uri url,
			IEndpointRouter endpointRouter,
			IServiceBus bus)
			: this(database, url, endpointRouter, bus, null)
		{

		}

		public DistributedHashTable(
			string database,
			Uri url,
			IEndpointRouter endpointRouter,
			IServiceBus bus,
			Node metadata)
		{
			Url = url;
			this.endpointRouter = endpointRouter;
			this.bus = bus;
			Metadata = metadata;

			if (Metadata != null) // sole node in the network, probably
			{
				Metadata.ExecuteSync(uri =>
				{
					ServiceUtil.Execute<IDistributedHashTableMetaDataProvider>(uri, srv =>
					{
						failOver = srv.GetNodeByUri(url);
					});
				});
			}
			try
			{
				hashTable = new PersistentHashTable(database);
				hashTable.Initialize();
			}
			catch (Exception)
			{
				hashTable.Dispose();
				throw;
			}
		}

		public Uri Url
		{
			get; private set;
		}

		public void Dispose()
		{
			hashTable.Dispose();
		    GC.SuppressFinalize(this);
		}

        ~DistributedHashTable()
        {
            try
            {
                hashTable.Dispose();
            }
            catch (Exception)
            {
                //not much I can do
            }
        }

		public PutResult[] Put(Node originalDestination, params PutRequest[] valuesToAdd)
		{
			var results = new List<PutResult>();
			using (var tx = new TransactionScope())
			{
				hashTable.Batch(actions =>
				{
					foreach (var request in valuesToAdd)
					{
						if(request.ParentVersions==null)
							throw new ArgumentException("Could not accept request with no ParentVersions");
						if (request.Key.StartsWith(rhinoDhtStartToken))
							throw new ArgumentException(rhinoDhtStartToken + " is a reserved key prefix");
						var put = actions.Put(request);
						//prepare the value for replication
						request.ReplicationVersion = put.Version;
						results.Add(put);
					}

					HandleReplication(originalDestination, new PutRequests {Requests = valuesToAdd});

					actions.Commit();
				});

				tx.Complete();
			}
			return results.ToArray();
		}

		private void SendToFailoverNodes(object msg)
		{
			if (failOver == null)
				return;
			if (failOver.Secondary != null)
				bus.Send(endpointRouter.GetRoutedEndpoint(failOver.Secondary.Async), msg);
			if (failOver.Tertiary != null)
				bus.Send(endpointRouter.GetRoutedEndpoint(failOver.Tertiary.Async), msg);
		}

		public bool[] Remove(Node originalDestination, params RemoveRequest[] valuesToRemove)
		{

			var results = new List<bool>();
			using (var tx = new TransactionScope())
			{
				hashTable.Batch(actions =>
				{
					foreach (var request in valuesToRemove)
					{
						if (request.ParentVersions == null)
							throw new ArgumentException("Could not accept request with no ParentVersions");
						
						var remove = actions.Remove(request);
						results.Add(remove);
					}

					HandleReplication(originalDestination, new RemoveRequests {Requests = valuesToRemove});
					actions.Commit();
				});

				tx.Complete();
			}
			return results.ToArray();
		}

		private void HandleReplication(
			Node originalDestination,
			object valueToSend)
		{
			//if this is the replication node, this is a replicated value,
			// and we don't need to do anything with replication
			if (originalDestination == Replication.Node) 
				return;

			// we replicate to our failover nodes
			if (originalDestination.Primary.Sync == Url)
				SendToFailoverNodes(valueToSend);
			else// if this got to us because of fail over
			{
				var primaryEndpoint = endpointRouter.GetRoutedEndpoint(originalDestination.Primary.Async);
				bus.Send(primaryEndpoint, valueToSend);
				var otherNode = originalDestination.GetOtherReplicationNode(Url);
				if(otherNode!=null)
				{
					var endpoint = endpointRouter.GetRoutedEndpoint(otherNode.Async);
					bus.Send(endpoint, valueToSend);
				}
			}
		}

		public Value[][] Get(params GetRequest[] valuesToGet)
		{
			var results = new List<Value[]>();
			hashTable.Batch(actions =>
			{
				foreach (var request in valuesToGet)
				{
					var values = actions.Get(request);
					results.Add(values);
				}

				actions.Commit();
			});
			return results.ToArray();
		}
	}
}