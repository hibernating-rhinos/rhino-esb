namespace Rhino.DistributedHashTable.Tests
{
	using System;
	using System.IO;
	using System.Messaging;
	using Castle.Windsor;
	using Castle.Windsor.Configuration;
	using Rhino.ServiceBus.Impl;
	using Rhino.ServiceBus.Msmq;
	using ServiceBus;

	public class DhtTestBase : IDisposable
	{
		private readonly IWindsorContainer container;
		protected readonly DhtBootStrapper bootStrapper;
		protected readonly DistributedHashTableClient client;
		protected static Uri metaDataUrl;
		protected readonly IStartableServiceBus bus;

		public DhtTestBase(IConfigurationInterpreter interpreter)
		{
			EnsureQueueExistsAndIsEmpty("msmq://localhost/dht_test.replication");
			EnsureQueueExistsAndIsEmpty("msmq://localhost/dht_test.replication2");
			EnsureQueueExistsAndIsEmpty("msmq://localhost/dht_test.replication3");

			Delete("cache.esent");
			Delete("cache1.esent");
			Delete("cache2.esent");
			Delete("cache3.esent");
			Delete("test.esent");

			container = new WindsorContainer(interpreter);
			var facility = new RhinoServiceBusFacility();
			bootStrapper = new DhtBootStrapper();
			bootStrapper.ConfigureBusFacility(facility);

			container.Kernel.AddFacility("rhino.esb", facility);

			bus = container.Resolve<IStartableServiceBus>();

			bootStrapper.InitializeContainer(container);

			bus.Start();

			bootStrapper.AfterStart();

			metaDataUrl = new Uri("net.tcp://localhost:8128/dht.metadata");
			client = new DistributedHashTableClient(new Node
			{
				Primary = new NodeUri
				{
					Sync = metaDataUrl
				}
			});
		}

		private static void EnsureQueueExistsAndIsEmpty(string queueUrl)
		{
			var path = MsmqUtil.GetQueuePath(new Endpoint{Uri = new Uri(queueUrl)});
			path.Create();
			using(var q = new MessageQueue(path.QueuePath))
				q.Purge();
		}

		public static void Delete(string database)
		{
			if (Directory.Exists(database))
				Directory.Delete(database, true);
		}

		public virtual void Dispose()
		{
			bootStrapper.Dispose();
			bus.Dispose();
		}
	}
}