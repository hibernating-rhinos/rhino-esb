namespace Rhino.DistributedHashTable.Consumers
{
	using System;
	using System.Diagnostics;
	using Messages;
	using ServiceBus;

	public class PutRequestsConsumer : ConsumerOf<PutRequests>
	{
		private readonly IDistributedHashTable distributedHashTable;

		public PutRequestsConsumer(IDistributedHashTable distributedHashTable)
		{
			this.distributedHashTable = distributedHashTable;
		}

		public void Consume(PutRequests message)
		{
			distributedHashTable.Put(Replication.Node, message.Requests);
		}
	}
}